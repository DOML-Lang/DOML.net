using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DOML.IR;
using DOML.Logger;
using DOML.AST;

namespace DOML.AST {
    /// <summary>
    /// An extremely simple tokenizer.
    /// </summary>
    public class Tokenizer {
        // Our reader
        public TextReader reader;

        public int line = 1;
        public int col = 0;
        public char currentChar = '\0';
        public bool isEOF = false;

        public Tokenizer(TextReader reader) {
            this.reader = reader;
            Advance();
        }

        public string AdvanceLine() {
            if (isEOF) return null;
            line++;
            line = 0;
            string outText = reader.ReadLine();
            if (outText == null) isEOF = true;
            return outText;
        }

        public bool AdvanceAndIgnoreWS(int amount) {
            if (!Advance(amount)) return false;
            return IgnoreWhitespace();
        }

        public bool AdvanceAndIgnoreWS() => AdvanceAndIgnoreWS(1);
        public bool Advance() => Advance(1);

        public bool Advance(int amount) {
            if (isEOF) return false;
            for (; amount > 1; amount--) reader.Read();

            int last = reader.Read();
            isEOF = last < 0;
            if (isEOF) return false;
            currentChar = (char)last;

            if (currentChar == '\n') {
                line++;
                col = 0;
            } else {
                col++;
            }
            return true;
        }

        public bool IgnoreWhitespace() {
            while (char.IsWhiteSpace(currentChar) && Advance()) ;
            return !isEOF;
        }
    }
}
