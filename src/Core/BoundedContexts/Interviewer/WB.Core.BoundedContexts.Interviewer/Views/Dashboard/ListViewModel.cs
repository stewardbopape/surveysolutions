using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MvvmCross.Core.ViewModels;
using WB.Core.BoundedContexts.Interviewer.Views.Dashboard.DashboardItems;

namespace WB.Core.BoundedContexts.Interviewer.Views.Dashboard
{
    public abstract class ListViewModel : InterviewTabPanel
    {
        public bool IsItemsLoaded { get; protected set; }
        public event EventHandler OnItemsLoaded;
        protected abstract IEnumerable<IDashboardItem> GetUiItems();

        private MvxObservableCollection<IDashboardItem> uiItems = new MvxObservableCollection<IDashboardItem>();
        public MvxObservableCollection<IDashboardItem> UiItems {
            get => this.uiItems;
            protected set => this.RaiseAndSetIfChanged(ref this.uiItems, value);
        }

        private int itemsCount;
        public int ItemsCount
        {
            get => this.itemsCount;
            protected set => this.RaiseAndSetIfChanged(ref this.itemsCount, value);
        }

        protected void UpdateUiItems() => Task.Run(() =>
        {
            Debug.WriteLine("------------START UPDATE UI ITEMS: " + this.GetType().Name + " ---------------");
            this.IsItemsLoaded = false;

            try
            {
                var newItems = this.GetUiItems();
                this.UiItems.ReplaceWith(newItems);
            }
            finally
            {
                this.IsItemsLoaded = true;
            }
            Debug.WriteLine("------------END UPDATE UI ITEMS: " + this.GetType().Name + " ---------------");
            this.OnItemsLoaded?.Invoke(this, EventArgs.Empty);
        });
    }
}