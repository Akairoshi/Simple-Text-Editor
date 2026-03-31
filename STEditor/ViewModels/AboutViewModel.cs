using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;

namespace STEditor.ViewModels
{
    public sealed class AboutViewModel : ViewModelBase
    {
        public ICommand OpenLinkCommand { get; }

        public AboutViewModel()
        {
            OpenLinkCommand = new RelayCommand(OpenLink);
        }


        private void OpenLink(object? parameter)
        {
            if (parameter == null)
                return;

            var url = parameter.ToString();

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
