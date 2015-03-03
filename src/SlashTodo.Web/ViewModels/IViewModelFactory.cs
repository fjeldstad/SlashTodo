using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Web.ViewModels
{
    public interface IViewModelFactory
    {
        TViewModel Create<TViewModel>() where TViewModel : ViewModelBase, new();
    }
}
