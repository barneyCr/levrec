using System;
using System.IO;
using System.Text;

namespace LeverageRECalculator
{
    public partial class Program
    {
        public static OutStream Out;
        public static InStream In;
        public static void DeviateTextStreams() {
            if (!Directory.Exists("logs")) {
                Directory.CreateDirectory("logs");
            }
            Program.Out = new OutStream("logs/" + DateTime.Now.DayOfWeek.ToString() + " - " + DateTime.Now.ToShortTimeString() + ".txt");
            Program.In = new InStream(Program.Out);
        }
    }

    public sealed class InStream : TextReader {
        private OutStream _out;
        public InStream(OutStream _out)
        {
            this._out = _out;
        }
        public override string ReadLine()
		{
            string read = Console.ReadLine();
            _out.writer.WriteLine(read);
            return read;
		}
	}

    public sealed class OutStream : TextWriter
    {
        public override Encoding Encoding => Console.OutputEncoding;
        internal StreamWriter writer;
        private TextWriter console;

        public OutStream(string path)
        {
            this.console = Console.Out;
            writer = new StreamWriter(path);
        }

        public override void WriteLine() {
            console.WriteLine();
            writer.WriteLine();
        }

		public override void WriteLine(int value)
		{
            //base.WriteLine(value);
            console.WriteLine(value);
            writer.WriteLine(value);
		}

        public override void WriteLine(bool value)
        {
            //base.WriteLine(value);
            console.WriteLine(value);
            writer.WriteLine(value);
        }public override void WriteLine(char value)
        {
            //base.WriteLine(value);
            console.WriteLine(value);
            writer.WriteLine(value);
        }
        public override void WriteLine(long value)
        {
            //base.WriteLine(value);
            console.WriteLine(value);
            writer.WriteLine(value);
        }
        public override void WriteLine(ulong value)
        {
            //base.WriteLine(value);
            console.WriteLine(value);
            writer.WriteLine(value);
        }
        public override void WriteLine(uint value)
        {
            //base.WriteLine(value);
            console.WriteLine(value);
            writer.WriteLine(value);
        }
		public override void WriteLine(float value)
		{
            //base.WriteLine(value);
            console.WriteLine(value);
            writer.WriteLine(value);
		}
		public override void WriteLine(double value)
		{
            //base.WriteLine(value);
            console.WriteLine(value);
            writer.WriteLine(value);
		}
		public override void WriteLine(object value)
		{
            //base.WriteLine(value);
            console.WriteLine(value);
            writer.WriteLine(value);
		}
		public override void WriteLine(string value)
		{
            //base.WriteLine(value);
            console.WriteLine(value);
            writer.WriteLine(value);
        }public override void WriteLine(decimal value)
        {
            //base.WriteLine(value);
            console.WriteLine(value);
            writer.WriteLine(value);
        }
		public override void WriteLine(char[] buffer)
		{
            //base.WriteLine(buffer);
            console.WriteLine(buffer);
            writer.WriteLine(buffer);
		}

		public override void WriteLine(string format, object arg0)
		{
            //base.WriteLine(format, arg0);
            console.WriteLine(format, arg0);
            writer.WriteLine(format, arg0);
		}
		public override void WriteLine(string format, object arg0, object arg1, object arg2)
		{
            //base.WriteLine(format, arg0, arg1, arg2);
            console.WriteLine(format, arg0, arg1, arg2);
            writer.WriteLine(format, arg0, arg1, arg2);

		}
		public override void WriteLine(string format, object arg0, object arg1)
		{
            //base.WriteLine(format, arg0, arg1);
            console.WriteLine(format, arg0, arg1);
            writer.WriteLine(format, arg0, arg1);
		}
		public override void WriteLine(string format, params object[] arg)
		{
            //base.WriteLine(format, arg);
            console.WriteLine(format, arg);
            writer.WriteLine(format, arg);
		}
		public override void WriteLine(char[] buffer, int index, int count)
		{
            //base.WriteLine(buffer, index, count);
            console.WriteLine(buffer, index, count);
            writer.WriteLine(buffer, index, count);
		}
		public override void Write(int value)
        {
            //base.Write(value);
            console.Write(value);
            writer.Write(value);
        }

        public override void Write(bool value)
        {
            //base.Write(value);
            console.Write(value);
            writer.Write(value);
        }
        public override void Write(char value)
        {
            //base.Write(value);
            console.Write(value);
            writer.Write(value);
        }
        public override void Write(long value)
        {
            //base.Write(value);
            console.Write(value);
            writer.Write(value);
        }
        public override void Write(ulong value)
        {
            //base.Write(value);
            console.Write(value);
            writer.Write(value);
        }
        public override void Write(uint value)
        {
            //base.Write(value);
            console.Write(value);
            writer.Write(value);
        }
        public override void Write(float value)
        {
            //base.Write(value);
            console.Write(value);
            writer.Write(value);
        }
        public override void Write(double value)
        {//base.Write(value);
            console.Write(value);
            writer.Write(value);
        }
        public override void Write(object value)
        {
            //base.Write(value);
            console.Write(value);
            writer.Write(value);
        }
        public override void Write(string value)
        {
            //base.Write(value);
            console.Write(value);
            writer.Write(value);
        }
        public override void Write(decimal value)
        {
            //base.Write(value);
            console.Write(value);
            writer.Write(value);
        }
        public override void Write(char[] buffer)
        {
            //base.Write(buffer);
            console.Write(buffer);
            writer.Write(buffer);
        }

        public override void Write(string format, object arg0)
        {
            //base.Write(format, arg0);
            console.Write(format, arg0);
            writer.Write(format, arg0);
        }
        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            //base.Write(format, arg0, arg1, arg2);
            console.Write(format, arg0, arg1, arg2);
            writer.Write(format, arg0, arg1, arg2);

        }
        public override void Write(string format, object arg0, object arg1)
        {
            //base.Write(format, arg0, arg1);
            console.Write(format, arg0, arg1);
            writer.Write(format, arg0, arg1);
        }
        public override void Write(string format, params object[] arg)
        {
            //base.Write(format, arg);
            console.Write(format, arg);
            writer.Write(format, arg);
        }
		public override void Write(char[] buffer, int index, int count)
		{
            //base.Write(buffer, index, count);
            console.Write(buffer, index, count);
            writer.Write(buffer, index, count);
		}

		public override void Flush()
		{
            console.Flush();
            writer.Flush();
		}

		public override void Close()
		{
            console.Close();
            writer.Close();
		}
	}
}
