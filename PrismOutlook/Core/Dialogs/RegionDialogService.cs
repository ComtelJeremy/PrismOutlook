using Prism.Ioc;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace PrismOutlook.Core.Dialogs
{
    //TODO: think about this some more
    public class RegionDialogService : IRegionDialogService
    {
        private readonly IContainerExtension _containerExtension;
        private readonly IRegionManager _regionManager;

        public RegionDialogService(IContainerExtension containerExtension, IRegionManager regionManager)
        {
            _containerExtension = containerExtension;
            _regionManager = regionManager;
        }

        public void Show(string regionName, string name)
        {
            var window = _containerExtension.Resolve<RibbonDialogWindow>();

            var scopedRegionManager = _regionManager.CreateRegionManager();
            RegionManager.SetRegionManager(window, scopedRegionManager);

            IRegion region = scopedRegionManager.Regions[regionName];

            IDialogAware GetActiveViewModel()
            {
                var activeView = region.ActiveViews.FirstOrDefault() as FrameworkElement;
                var dialogAware = activeView?.DataContext as IDialogAware ??
                                           throw new InvalidOperationException("View in RegionDialog must be IDialogAware.");
                return dialogAware;
            }

            Action<IDialogResult> requestCloseHandler = o =>
            {
                window.Close();
            };

            CancelEventHandler closingHandler = (o, e) =>
            {
                var dialogAware = GetActiveViewModel();
                if (!dialogAware.CanCloseDialog())
                {
                    e.Cancel = true;
                }
            };

            // NOTE: Don't need this handler, since it is taken care of on Navigate below.
            
            /*RoutedEventHandler loadedHandler = null;
            loadedHandler = (o, e) =>
            {
                window.Loaded -= loadedHandler;
                var dialogAware = o as IDialogAware;
                dialogAware.RequestClose += requestCloseHandler;
            };*/

            EventHandler closedHandler = null;
            closedHandler = (o, e) =>
            {
                var dialogAware = GetActiveViewModel();
                // TOOD: Not 100% sure that this will unregister itself as it might not be captured in the lambda,
                //          but can't find a memory leak... only registered once per window, not per view.
                window.Closed -= closedHandler;
                window.Closing -= closingHandler;
                
                dialogAware.RequestClose -= requestCloseHandler;

                window.DataContext = null;
                window.Content = null;
            };

            // NOTE: Moved this here, since it is attached to the window and only, not view
            // we are now using the local function GetActiveViewModel() to find the relevant VM.
            window.Closing += closingHandler;
            window.Closed += closedHandler;

            region.ActiveViews.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (var view in args.NewItems)
                    {
                        var dialogAware = ((FrameworkElement) view).DataContext as IDialogAware;
                        //TODO: Do we need a check here if the DataContext is IDialogAware or not?
                        dialogAware.RequestClose += requestCloseHandler;

                        // Not needed, see note above.
                        //window.Loaded += loadedHandler;
                    }
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (var view in args.OldItems)
                    {
                        var dialogAware = ((FrameworkElement) view).DataContext as IDialogAware;
                        //TODO: Do we need a check here if the DataContext is IDialogAware or not?
                        dialogAware.RequestClose -= requestCloseHandler;
                    }
                }
            };

            scopedRegionManager.RequestNavigate(regionName, name);


            //Action<IDialogResult> requestCloseHandler = null;
            //requestCloseHandler = (o) =>
            //{
            //    window.Close();
            //};

            //CancelEventHandler closingHandler = null;
            //closingHandler = (o, e) =>
            //{
            //    if (!dialogAware.CanCloseDialog())
            //        e.Cancel = true;
            //};
            //window.Closing += closingHandler;

            //RoutedEventHandler loadedHandler = null;
            //loadedHandler = (o, e) =>
            //{
            //    window.Loaded -= loadedHandler;
            //    dialogAware.RequestClose += requestCloseHandler;
            //};
            //window.Loaded += loadedHandler;

            //EventHandler closedHandler = null;
            //closedHandler = (o, e) =>
            //{
            //    window.Closed -= closedHandler;
            //    window.Closing -= closingHandler;

            //    window.DataContext = null;
            //    window.Content = null;
            //};
            //window.Closed += closedHandler;
            window.Owner = Application.Current.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }
    }
}
