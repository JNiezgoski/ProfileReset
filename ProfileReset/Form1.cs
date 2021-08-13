using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProfileReset
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
    }
}


      public async Task GetUserProfiles()
{​
            // Instantiate a token source to handle user cancellation
            var tokenSource = new CancellationTokenSource();
    var viewModel = new DetailedProgressDialogViewModel();
    var view = ViewLocator.LocateForModel(viewModel, null, null);
    ViewModelBinder.Bind(viewModel, view, null);
    UserProfiles = new BindableCollection<UserProfile>();
    CanResetProfiles = true;
    UserProfile loadedProfile = null;
    await DialogHost.Show(view, "RootDialog", (object sender, DialogOpenedEventArgs args) =>
    {​
                Task.Run(async () =>
                {​
                    var result = await _wmiService.GetUserProfiles(ADUser, RDSServers, (object s, DetailedProgressEventArgs a) =>
                    {​
                        viewModel.ProgressChanged(a.Progress, a.Status);
                    }​, tokenSource.Token);
                    if (!tokenSource.IsCancellationRequested && result.Success)
                    {​
                        foreach (var profile in result.Content)
                        {​
                            profile.UPDStatus = await _registryService.IsUvhdEnabledAsync(profile.Server.ServerFQDN);
                            if (profile.Loaded || profile.IsUPDMounted) loadedProfile = profile;
                            UserProfiles.Add(profile);
                        }​
                        // Check if even one profile is using UPDs, and set the UPD status to enabled if found
                        if (UserProfiles.Any(p => p.UPDStatus == UPDStatus.Enabled))
                        {​
                            UPDStatus = UPDStatus.Enabled;
                        }​
                        else if (UserProfiles.Any(p => p.UPDStatus == UPDStatus.Disabled))
                        {​
                            UPDStatus = UPDStatus.Disabled;
                        }​
                        NotifyOfPropertyChange(() => UserProfiles);
                    }​
                }​).ContinueWith((t, _) =>
                {​
                    if (t.IsFaulted)
                    {​
                        var message = new MessageDialogView()
                        {​
                            Message = {​ Text = $"Error: {​t.Exception.InnerException.Message}​" }​
                        }​;
                        args.Session.UpdateContent(message);
                    }​
                    else
                    {​
                        if (!tokenSource.IsCancellationRequested) args.Session.Close("");
                        if (UserProfiles.Count > 0)
                        {​
                            var index = 0;
                            if (loadedProfile != null)
                            {​
                                index = UserProfiles.IndexOf(loadedProfile);
                            }​
                            ProfileQueryCompleteHandler.Invoke(this, new ProfileQueryCompleteEventArgs(index));
                        }​
                    }​
                }​, null, TaskScheduler.FromCurrentSynchronizationContext());
    }​, (object sender, DialogClosingEventArgs args) =>
    {​
                // This is the event handler for when the dialog is closed
                // This just checks if the close command came from the program or the user
                var param = args.Parameter as string;
        if (param.IsNullOrWhiteSpace()) return;
        if (param.ToLower() == "cancel")
        {​
                    // If user cancelled, call for cancellation on the token that was passed in
                    tokenSource.Cancel();
        }​
            }​);
}​
    
    
  
  

