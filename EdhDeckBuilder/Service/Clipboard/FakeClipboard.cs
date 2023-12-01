using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service.Clipboard
{
    public class FakeClipboard : IClipboard
    {
        private string _text = "";
        
        public string GetClipboardText()
        {
            return _text;
        }

        public void SetClipboardText(string text)
        {
            _text = text;
        }

        public void SetText(string text)
        {
            _text = text;
        }
    }
}
