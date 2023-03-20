﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Humanizer;
using Humanizer.Localisation;
using MvvmCross.Base;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Utils;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;
using Xamarin.Essentials;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions
{
    public class AudioQuestionViewModel :
        MvxNotifyPropertyChanged,
        IInterviewEntityViewModel,
        IViewModelEventHandler<AnswersRemoved>,
        ICompositeQuestion,
        IDisposable
    {
        private readonly IPrincipal principal;
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly QuestionStateViewModel<AudioQuestionAnswered> questionState;

        private Identity questionIdentity;
        private Guid interviewId;
        private string variableName;

        private readonly IViewModelEventRegistry liteEventRegistry;
        private readonly IPermissionsService permissions;
        private readonly IAudioDialog audioDialog;
        private readonly IAudioFileStorage audioFileStorage;
        private readonly IAudioService audioService;
        private readonly IMvxMainThreadAsyncDispatcher mainThreadAsyncDispatcher;
        public AnsweringViewModel Answering { get; }

        public AudioQuestionViewModel(
            IPrincipal principal,
            IStatefulInterviewRepository interviewRepository,
            IQuestionnaireStorage questionnaireStorage,
            QuestionStateViewModel<AudioQuestionAnswered> questionStateViewModel,
            AnsweringViewModel answering,
            QuestionInstructionViewModel instructionViewModel,
            IViewModelEventRegistry liteEventRegistry,
            IPermissionsService permissions,
            IAudioDialog audioDialog,
            IAudioFileStorage audioFileStorage,
            IAudioService audioService,
            IMvxMainThreadAsyncDispatcher mainThreadAsyncDispatcher)
        {
            this.principal = principal;
            this.interviewRepository = interviewRepository;
            this.questionnaireStorage = questionnaireStorage;

            this.questionState = questionStateViewModel;
            this.InstructionViewModel = instructionViewModel;
            this.Answering = answering;
            this.liteEventRegistry = liteEventRegistry;
            this.permissions = permissions;
            this.audioDialog = audioDialog;
            this.audioFileStorage = audioFileStorage;
            this.audioService = audioService;
            this.mainThreadAsyncDispatcher = mainThreadAsyncDispatcher;

            this.audioService.OnPlaybackCompleted += OnPlaybackCompleted;
        }

        private void OnPlaybackCompleted(object sender, PlaybackCompletedEventArgs e)
        {
            this.IsPlaying = false;
        }

        public bool IsPlaying
        {
            get => isPlaying;
            set
            {
                if (value == isPlaying) return;
                isPlaying = value;
                RaisePropertyChanged(() => IsPlaying);
            }
        }

        public bool CanBePlayed
        {
            get => canBePlayed;
            private set
            {
                if (value == canBePlayed) return;
                canBePlayed = value;
                RaisePropertyChanged(() => CanBePlayed);
            }
        }

        public IQuestionStateViewModel QuestionState => this.questionState;

        public QuestionInstructionViewModel InstructionViewModel { get; }

        public Identity Identity => this.questionIdentity;

        private string answer;
        private bool isPlaying;
        private bool canBePlayed;

        public string Answer
        {
            get => this.answer;
            set { this.answer = value; this.RaisePropertyChanged(); }
        }

        public ICommand RecordAudioCommand => new MvxAsyncCommand(this.RecordAudioAsync);

        public IMvxAsyncCommand RemoveAnswerCommand => new MvxAsyncCommand(this.RemoveAnswerAsync);

        public IMvxCommand TogglePlayback => new MvxAsyncCommand(async () =>
        {
            if (this.IsPlaying)
            {
                this.audioService.Stop();
                this.IsPlaying = false;
            }
            else
            {
                var interviewBinaryData = await this.audioFileStorage.GetInterviewBinaryDataAsync(interviewId, this.GetAudioFileName());
                this.audioService.Play(interviewBinaryData, this.questionIdentity);
                this.IsPlaying = true;
            }
        });
        
        public void Init(string interviewId, Identity entityIdentity, NavigationState navigationState)
        {
            this.QuestionState.Init(interviewId, entityIdentity, navigationState);
            this.InstructionViewModel.Init(interviewId, entityIdentity, navigationState);

            this.questionIdentity = entityIdentity;

            var interview = this.interviewRepository.Get(interviewId);
            this.interviewId = interview.Id;
            var questionnaire = this.questionnaireStorage.GetQuestionnaire(interview.QuestionnaireIdentity, interview.Language);
            this.variableName = questionnaire.GetQuestionVariableName(entityIdentity.Id);

            var answerModel = interview.GetAudioQuestion(entityIdentity);
            if (answerModel.IsAnswered())
            {
                this.SetAnswer(answerModel.GetAnswer().Length);
            }

            this.liteEventRegistry.Subscribe(this, interviewId);
        }

        private async Task SendAnswerAsync()
        {
            var audioDuration = this.audioService.GetAudioRecordDuration();

            var command = new AnswerAudioQuestionCommand(
                interviewId: this.interviewId,
                userId: this.principal.CurrentUserIdentity.UserId,
                questionId: this.questionIdentity.Id,
                rosterVector: this.questionIdentity.RosterVector,
                fileName: this.GetAudioFileName(),
                length: audioDuration);

            try
            {
                var audioStream = this.audioService.GetRecord();

                using (var audioMemoryStream = new MemoryStream())
                {
                    audioStream.CopyTo(audioMemoryStream);
                    this.audioFileStorage.StoreInterviewBinaryData(this.interviewId, this.GetAudioFileName(),
                        audioMemoryStream.ToArray(), this.audioService.GetMimeType());
                }

                await this.Answering.SendQuestionCommandAsync(command);

                this.SetAnswer(audioDuration);

                await this.QuestionState.Validity.ExecutedWithoutExceptions();
            }
            catch (InterviewException ex)
            {
                await this.QuestionState.Validity.ProcessException(ex);
            }
        }

        private void SetAnswer(TimeSpan duration)
        {
            this.Answer = string.Format(UIResources.AudioQuestion_DurationFormat,
                duration.Humanize(maxUnit: TimeUnit.Minute, minUnit: TimeUnit.Second));
            
            this.CanBePlayed = this.audioFileStorage.GetInterviewBinaryData(this.interviewId, GetAudioFileName()) != null;
        }

        private async Task RecordAudioAsync()
        {
            if (this.audioService.IsAnswerRecording()) return;
            this.audioService.Stop();
            this.IsPlaying = false;

            try
            {
                await this.permissions.AssureHasPermissionOrThrow<Permissions.Microphone>().ConfigureAwait(false);

                this.audioDialog.OnRecorded += this.AudioDialog_OnRecorded;
                this.audioDialog.OnCancelRecording += AudioDialog_OnCancel;

                await mainThreadAsyncDispatcher.ExecuteOnMainThreadAsync(() =>
                    this.audioDialog.ShowAndStartRecording(this.QuestionState.Header.Title.HtmlText));
            }
            catch (MissingPermissionsException e) when (e.PermissionType == typeof(Permissions.Microphone))
            {
                await this.QuestionState.Validity.MarkAnswerAsNotSavedWithMessage(UIResources
                    .MissingPermissions_Microphone);
            }
            catch (MissingPermissionsException e) when (e.PermissionType == typeof(Permissions.StorageWrite))
            {
                await this.QuestionState.Validity.MarkAnswerAsNotSavedWithMessage(UIResources
                    .MissingPermissions_Storage);
            }
            catch (MissingPermissionsException e)
            {
                await this.QuestionState.Validity.MarkAnswerAsNotSavedWithMessage(e.Message);
            }
            catch (AudioException e) when (e.Type == AudioExceptionType.Io)
            {
                await this.QuestionState.Validity.MarkAnswerAsNotSavedWithMessage(UIResources.Audio_Io_Exception_Message);
            }
            catch (AudioException e) when (e.Type == AudioExceptionType.Unhandled)
            {
                await this.QuestionState.Validity.MarkAnswerAsNotSavedWithMessage(UIResources.Audio_Unhandled_Exception_Message);
            }
        }

        private void AudioDialog_OnCancel(object sender, EventArgs e) => this.UnsubscribeDialog();

        private async void AudioDialog_OnRecorded(object sender, EventArgs e)
        {
            this.UnsubscribeDialog();

            if (this.QuestionState.Enablement.Enabled)
                await this.SendAnswerAsync();
        }

        private void UnsubscribeDialog()
        {
            this.audioDialog.OnRecorded -= this.AudioDialog_OnRecorded;
            this.audioDialog.OnCancelRecording -= this.AudioDialog_OnCancel;
        }

        private async Task RemoveAnswerAsync()
        {
            try
            {
                if(this.IsPlaying) this.TogglePlayback.Execute();

                await this.Answering.SendQuestionCommandAsync(
                    new RemoveAnswerCommand(this.interviewId,
                        this.principal.CurrentUserIdentity.UserId,
                        this.questionIdentity));
                await this.QuestionState.Validity.ExecutedWithoutExceptions();
            }
            catch (InterviewException exception)
            {
                await this.QuestionState.Validity.ProcessException(exception);
            }
        }

        public void Handle(AnswersRemoved @event)
        {
            if (!@event.Questions.Any(question => this.questionIdentity.Equals(question.Id, question.RosterVector)))
                return;

            this.Answer = null;
            this.audioFileStorage.RemoveInterviewBinaryData(this.interviewId, this.GetAudioFileName());
        }

        private string GetAudioFileName() => $"{this.variableName}__{this.questionIdentity.RosterVector}.{this.audioService.GetAudioType()}";

        public void Dispose()
        {
            this.audioService.OnPlaybackCompleted -= OnPlaybackCompleted;
            this.liteEventRegistry.Unsubscribe(this);
            this.audioService?.Dispose();
            this.QuestionState.Dispose();
            this.InstructionViewModel.Dispose();
        }
    }
}
