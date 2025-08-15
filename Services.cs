using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App3
{
    public class Messenger
    {
        public event Action<NavigationItem> NavigationItemAdded;
        private static Messenger _instance;
        public static Messenger Instance => _instance ??= new Messenger();
        public void AddNavigationItem(NavigationItem item)
        {
            NavigationItemAdded?.Invoke(item);
        }
    }
}
