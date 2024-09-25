using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV.Db.Sloth.SqlParser
{
    public class Symbols
    {
        /// <summary>
        /// '\n'
        /// </summary>
        public const char NewLine = '\n';

        /// <summary>
        /// ' '
        /// </summary>
        public const char Whitespace = ' ';

        /// <summary>
        /// '\r'
        /// </summary>
        public const char CarriageReturn = '\r';

        /// <summary>
        /// '<'
        /// </summary>
        public const char LessThan = (char)0x3c;

        /// <summary>
        /// '>'
        /// </summary>
        public const char GreaterThan = (char)0x3e;

        /// <summary>
        /// '='
        /// </summary>
        public const char Equal = (char)0x3d;

        /// <summary>
        /// '   '
        /// </summary>
        public const char Tab = (char)0x09;

        /// <summary>
        /// ','
        /// </summary>
        public const char Comma = (char)0x2c;

        /// <summary>
        /// ';'
        /// </summary>
        public const char Semicolon = (char)0x3b;

        /// <summary>
        /// '('
        /// </summary>
        public const char ParenOpen = (char)0x28;

        /// <summary>
        /// ')'
        /// </summary>
        public const char ParenClose = (char)0x29;

        /// <summary>
        /// '!'
        /// </summary>
        public const char ExclamationMark = (char)0x21;

        public const char EOF = char.MaxValue;
        public const char Zero = (char)0x30;                // 0
        public const char Nine = (char)0x39;                // 9
        public const char CapitalA = (char)0x41;            // A
        public const char CapitalF = (char)0x46;            // F
        public const char CapitalZ = (char)0x5a;            // Z
        public const char LowerA = (char)0x61;              // a
        public const char LowerF = (char)0x66;              // f
        public const char LowerZ = (char)0x7a;              // z
        public const char Tilde = (char)0x7e;               // ~
        public const char Pipe = (char)0x7c;                // |
        public const char Backtick = (char)0x60;            // `
        public const char Ampersand = (char)0x26;           // &amp
        public const char Num = (char)0x23;                 // #
        public const char Dollar = (char)0x24;              // $
        public const char Asterisk = (char)0x2a;            // *
        public const char Plus = (char)0x2b;                // +
        public const char Minus = (char)0x2d;               // -
        public const char Divide = (char)0x2f;              // /
        public const char Dot = (char)0x2e;                 // .
        public const char Caret = (char)0x5e;               // ^
        public const char At = (char)0x40;                  // @
        public const char SingleQuote = (char)0x27;         // '
        public const char DoubleQuote = (char)0x22;         // "
        public const char QuestionMark = (char)0x3f;        // ?
        public const char Backslash = (char)0x5c;           // reverse solidus \
        public const char Colon = (char)0x3a;               // :
        public const char Underscore = (char)0x5f;          // _
        public const char Percent = (char)0x25;             // %
        public const char SquareBracketOpen = (char)0x5b;   // [
        public const char SquareBracketClose = (char)0x5d;  // ]
        public const char CurlyBracketOpen = (char)0x7b;    // {
        public const char CurlyBracketClose = (char)0x7d;   // }
    }
}