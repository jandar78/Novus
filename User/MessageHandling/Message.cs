using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientHandling {
    public class Message {
        private List<string> _messages;

        public string Self {
            get {
                return _messages[0];
            }
            set {
                _messages[0] = value;
            }
        }

        public string Target {
            get {
                return _messages[1];
            }
            set {
                _messages[1] = value;
            }
        }

        public string Room {
            get {
                return _messages[2];
            }
            set {
                _messages[2] = value;
            }
        }

        public Message() {
            _messages = new List<string>();
        }

        /// <summary>
        /// Hold the messages with easy the get properties rather than using an index on a list.  
        /// Messages must have three elements, make them an empty string if necessary.
        /// </summary>
        /// <param name="messages"></param>
        public Message(List<string> messages){
            _messages = new List<string>();
            Self = string.IsNullOrEmpty(messages[0]) ? null : messages[0];
            Target = string.IsNullOrEmpty(messages[1]) ? null : messages[1];
            Room = string.IsNullOrEmpty(messages[2]) ? null : messages[2];
        }
    }
}
