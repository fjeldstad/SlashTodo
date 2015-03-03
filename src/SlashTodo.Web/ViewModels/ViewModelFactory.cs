using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlashTodo.Web.ViewModels
{
    public class ViewModelFactory : IViewModelFactory
    {
        public TViewModel Create<TViewModel>() where TViewModel : ViewModelBase, new()
        {
            var viewModel = new TViewModel();
            return viewModel;
        }
    }
}