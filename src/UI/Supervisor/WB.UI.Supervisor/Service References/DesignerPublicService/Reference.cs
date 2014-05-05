﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34011
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WB.UI.Supervisor.DesignerPublicService {
    using System.Runtime.Serialization;
    using System;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [DataContract(Name="QuestionnaireVersion", Namespace="http://schemas.datacontract.org/2004/07/WB.Core.SharedKernels.QuestionnaireVerifi" +
        "cation.ValueObjects")]
    [Serializable()]
    public partial class QuestionnaireVersion : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [NonSerialized()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [OptionalField()]
        private int MajorField;
        
        [OptionalField()]
        private int MinorField;
        
        [OptionalField()]
        private int PatchField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [DataMember()]
        public int Major {
            get {
                return this.MajorField;
            }
            set {
                if ((this.MajorField.Equals(value) != true)) {
                    this.MajorField = value;
                    this.RaisePropertyChanged("Major");
                }
            }
        }
        
        [DataMember()]
        public int Minor {
            get {
                return this.MinorField;
            }
            set {
                if ((this.MinorField.Equals(value) != true)) {
                    this.MinorField = value;
                    this.RaisePropertyChanged("Minor");
                }
            }
        }
        
        [DataMember()]
        public int Patch {
            get {
                return this.PatchField;
            }
            set {
                if ((this.PatchField.Equals(value) != true)) {
                    this.PatchField = value;
                    this.RaisePropertyChanged("Patch");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [DataContract(Name="QuestionnaireListViewItemMessage", Namespace="http://schemas.datacontract.org/2004/07/WB.UI.Designer.WebServices.Questionnaire")]
    [Serializable()]
    public partial class QuestionnaireListViewItemMessage : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [NonSerialized()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [OptionalField()]
        private System.Guid IdField;
        
        [OptionalField()]
        private string TitleField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [DataMember()]
        public System.Guid Id {
            get {
                return this.IdField;
            }
            set {
                if ((this.IdField.Equals(value) != true)) {
                    this.IdField = value;
                    this.RaisePropertyChanged("Id");
                }
            }
        }
        
        [DataMember()]
        public string Title {
            get {
                return this.TitleField;
            }
            set {
                if ((object.ReferenceEquals(this.TitleField, value) != true)) {
                    this.TitleField = value;
                    this.RaisePropertyChanged("Title");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="DesignerPublicService.IPublicService")]
    public interface IPublicService {
        
        // CODEGEN: Generating message contract since the wrapper name (DownloadQuestionnaireRequest) of message DownloadQuestionnaireRequest does not match the default value (DownloadQuestionnaire)
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IPublicService/DownloadQuestionnaire", ReplyAction="http://tempuri.org/IPublicService/DownloadQuestionnaireResponse")]
        RemoteFileInfo DownloadQuestionnaire(DownloadQuestionnaireRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IPublicService/DownloadQuestionnaire", ReplyAction="http://tempuri.org/IPublicService/DownloadQuestionnaireResponse")]
        System.Threading.Tasks.Task<RemoteFileInfo> DownloadQuestionnaireAsync(DownloadQuestionnaireRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IPublicService/DownloadQuestionnaireSource", ReplyAction="http://tempuri.org/IPublicService/DownloadQuestionnaireSourceResponse")]
        string DownloadQuestionnaireSource(System.Guid request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IPublicService/DownloadQuestionnaireSource", ReplyAction="http://tempuri.org/IPublicService/DownloadQuestionnaireSourceResponse")]
        System.Threading.Tasks.Task<string> DownloadQuestionnaireSourceAsync(System.Guid request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IPublicService/Dummy", ReplyAction="http://tempuri.org/IPublicService/DummyResponse")]
        void Dummy();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IPublicService/Dummy", ReplyAction="http://tempuri.org/IPublicService/DummyResponse")]
        System.Threading.Tasks.Task DummyAsync();
        
        // CODEGEN: Generating message contract since the wrapper name (QuestionnaireListRequest) of message QuestionnaireListRequest does not match the default value (GetQuestionnaireList)
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IPublicService/GetQuestionnaireList", ReplyAction="http://tempuri.org/IPublicService/GetQuestionnaireListResponse")]
        QuestionnaireListViewMessage GetQuestionnaireList(QuestionnaireListRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IPublicService/GetQuestionnaireList", ReplyAction="http://tempuri.org/IPublicService/GetQuestionnaireListResponse")]
        System.Threading.Tasks.Task<QuestionnaireListViewMessage> GetQuestionnaireListAsync(QuestionnaireListRequest request);
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="DownloadQuestionnaireRequest", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class DownloadQuestionnaireRequest {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public System.Guid QuestionnaireId;
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public QuestionnaireVersion SupportedQuestionnaireVersion;
        
        public DownloadQuestionnaireRequest() {
        }
        
        public DownloadQuestionnaireRequest(System.Guid QuestionnaireId, QuestionnaireVersion SupportedQuestionnaireVersion) {
            this.QuestionnaireId = QuestionnaireId;
            this.SupportedQuestionnaireVersion = SupportedQuestionnaireVersion;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="RemoteFileInfo", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class RemoteFileInfo {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public string FileName;
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public long Length;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
        public System.IO.Stream FileByteStream;
        
        public RemoteFileInfo() {
        }
        
        public RemoteFileInfo(string FileName, long Length, System.IO.Stream FileByteStream) {
            this.FileName = FileName;
            this.Length = Length;
            this.FileByteStream = FileByteStream;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="QuestionnaireListRequest", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class QuestionnaireListRequest {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public string Filter;
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public int PageIndex;
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public int PageSize;
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public string SortOrder;
        
        public QuestionnaireListRequest() {
        }
        
        public QuestionnaireListRequest(string Filter, int PageIndex, int PageSize, string SortOrder) {
            this.Filter = Filter;
            this.PageIndex = PageIndex;
            this.PageSize = PageSize;
            this.SortOrder = SortOrder;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="QuestionnaireListViewMessage", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class QuestionnaireListViewMessage {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public string Order;
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public int Page;
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public int PageSize;
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public int TotalCount;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
        public QuestionnaireListViewItemMessage[] Items;
        
        public QuestionnaireListViewMessage() {
        }
        
        public QuestionnaireListViewMessage(string Order, int Page, int PageSize, int TotalCount, QuestionnaireListViewItemMessage[] Items) {
            this.Order = Order;
            this.Page = Page;
            this.PageSize = PageSize;
            this.TotalCount = TotalCount;
            this.Items = Items;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IPublicServiceChannel : IPublicService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class PublicServiceClient : System.ServiceModel.ClientBase<IPublicService>, IPublicService {
        
        public PublicServiceClient() {
        }
        
        public PublicServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public PublicServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public PublicServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public PublicServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        RemoteFileInfo IPublicService.DownloadQuestionnaire(DownloadQuestionnaireRequest request) {
            return base.Channel.DownloadQuestionnaire(request);
        }
        
        public string DownloadQuestionnaire(System.Guid QuestionnaireId, QuestionnaireVersion SupportedQuestionnaireVersion, out long Length, out System.IO.Stream FileByteStream) {
            DownloadQuestionnaireRequest inValue = new DownloadQuestionnaireRequest();
            inValue.QuestionnaireId = QuestionnaireId;
            inValue.SupportedQuestionnaireVersion = SupportedQuestionnaireVersion;
            RemoteFileInfo retVal = ((IPublicService)(this)).DownloadQuestionnaire(inValue);
            Length = retVal.Length;
            FileByteStream = retVal.FileByteStream;
            return retVal.FileName;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<RemoteFileInfo> IPublicService.DownloadQuestionnaireAsync(DownloadQuestionnaireRequest request) {
            return base.Channel.DownloadQuestionnaireAsync(request);
        }
        
        public System.Threading.Tasks.Task<RemoteFileInfo> DownloadQuestionnaireAsync(System.Guid QuestionnaireId, QuestionnaireVersion SupportedQuestionnaireVersion) {
            DownloadQuestionnaireRequest inValue = new DownloadQuestionnaireRequest();
            inValue.QuestionnaireId = QuestionnaireId;
            inValue.SupportedQuestionnaireVersion = SupportedQuestionnaireVersion;
            return ((IPublicService)(this)).DownloadQuestionnaireAsync(inValue);
        }
        
        public string DownloadQuestionnaireSource(System.Guid request) {
            return base.Channel.DownloadQuestionnaireSource(request);
        }
        
        public System.Threading.Tasks.Task<string> DownloadQuestionnaireSourceAsync(System.Guid request) {
            return base.Channel.DownloadQuestionnaireSourceAsync(request);
        }
        
        public void Dummy() {
            base.Channel.Dummy();
        }
        
        public System.Threading.Tasks.Task DummyAsync() {
            return base.Channel.DummyAsync();
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        QuestionnaireListViewMessage IPublicService.GetQuestionnaireList(QuestionnaireListRequest request) {
            return base.Channel.GetQuestionnaireList(request);
        }
        
        public string GetQuestionnaireList(string Filter, int PageIndex, ref int PageSize, string SortOrder, out int Page, out int TotalCount, out QuestionnaireListViewItemMessage[] Items) {
            QuestionnaireListRequest inValue = new QuestionnaireListRequest();
            inValue.Filter = Filter;
            inValue.PageIndex = PageIndex;
            inValue.PageSize = PageSize;
            inValue.SortOrder = SortOrder;
            QuestionnaireListViewMessage retVal = ((IPublicService)(this)).GetQuestionnaireList(inValue);
            Page = retVal.Page;
            PageSize = retVal.PageSize;
            TotalCount = retVal.TotalCount;
            Items = retVal.Items;
            return retVal.Order;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<QuestionnaireListViewMessage> IPublicService.GetQuestionnaireListAsync(QuestionnaireListRequest request) {
            return base.Channel.GetQuestionnaireListAsync(request);
        }
        
        public System.Threading.Tasks.Task<QuestionnaireListViewMessage> GetQuestionnaireListAsync(string Filter, int PageIndex, int PageSize, string SortOrder) {
            QuestionnaireListRequest inValue = new QuestionnaireListRequest();
            inValue.Filter = Filter;
            inValue.PageIndex = PageIndex;
            inValue.PageSize = PageSize;
            inValue.SortOrder = SortOrder;
            return ((IPublicService)(this)).GetQuestionnaireListAsync(inValue);
        }
    }
}
