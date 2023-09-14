using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jeopardy_Websocket {
    public class Question {
        public string? Category { get; set; }
        public int? Number { get; set; }
        public string? Text { get; set; }
        public string? Answer { get; set; }
    }

}
