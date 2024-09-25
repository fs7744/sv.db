namespace SV.Db.Sloth.SqlParser
{
    public class ParserContext
    {
        private string data;
        private int index;
        public bool HasNext { get; private set; }
        public int Index { get => index; }
        public string Data { get => data; }

        public int Line { get; private set; }

        public int Column { get; private set; }

        public void NewLine()
        {
            Line += 1;
            Column = 1;
        }

        public void MoveColumn()
        {
            Column += 1;
        }

        public ParserContext(string sql)
        {
            this.data = sql;
            this.HasNext = data.Length > 0;
        }

        public bool TryNext(out char character)
        {
            if (HasNext)
            {
                index++;
                HasNext = index < data.Length;
                if (TryPeek(out character))
                {
                    if (character == Symbols.NewLine)
                    {
                        NewLine();
                    }
                    else
                    {
                        MoveColumn();
                    }
                    return true;
                }
            }
            else
            {
                character = char.MaxValue;
            }
            return false;
        }

        public bool TryPeek(out char character)
        {
            if (index < data.Length)
            {
                character = Peek();
                return true;
            }
            else
            {
                character = char.MaxValue;
                return false;
            }
        }

        public char Peek()
        {
            return data[index];
        }

        public string GetSomeChars()
        {
            if (HasNext)
            {
                return data.Substring(index, Math.Min(5, data.Length - index - 1));
            }
            else
            {
                return string.Empty;
            }
        }
    }
}