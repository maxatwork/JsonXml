using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace JsonXml
{
	public class TextJsonReader : TextReader
	{
		private enum State
		{
			Unknown,
			Property,
			ArrayStart,
			Array,
			ObjectStart,
			Object
		}

		private readonly JsonReader _reader;
		private readonly StringBuilder _json;
		private readonly Stack<State> _states;
		private bool _disposed;
		private bool _closed;

		public TextJsonReader(JsonReader reader)
		{
			_closed = false;
			_disposed = false;
			_reader = reader;
			_json = new StringBuilder();
			_states = new Stack<State>();
		}

		public override int Read(char[] buffer, int index, int count)
		{
			if (index + count > buffer.Length) 
				throw new ArgumentException("buffer is too small", "buffer");

			while (_json.Length < count && _reader.Read())
			{
				var state = _states.Count > 0 ? _states.Peek() : State.Unknown;
				
				switch (_reader.TokenType)
				{
					case JsonToken.PropertyName:
						if (state != State.ObjectStart) _json.Append(",");
						_json.AppendFormat("\"{0}\":", _reader.Value.ToString().Replace("\"", "\\\""));
						_states.Push(State.Property);
						break;

					case JsonToken.StartObject:
						CheckAndInsertComma();
						_json.Append("{");
						_states.Push(State.ObjectStart);
						break;

					case JsonToken.StartArray:
						CheckAndInsertComma();
						_json.Append("[");
						_states.Push(State.ArrayStart);
						break;

					case JsonToken.String:
						CheckAndInsertComma();
						_json.AppendFormat("\"{0}\"", _reader.Value.ToString().Replace("\"", "\\\""));
						break;

					case JsonToken.Integer:
						CheckAndInsertComma();
						_json.Append(_reader.Value);
						break;

					case JsonToken.Float:
						CheckAndInsertComma();
						_json.Append(((double)_reader.Value).ToString(CultureInfo.InvariantCulture.NumberFormat));
						break;

					case JsonToken.Boolean:
						CheckAndInsertComma();
						_json.Append((bool)_reader.Value ? "true" : "false");
						break;

					case JsonToken.Null:
						CheckAndInsertComma();
						_json.Append("null");
						break;

					case JsonToken.EndObject:
						_json.Append("}");
						_states.Pop();
						break;

					case JsonToken.EndArray:
						_json.Append("]");
						_states.Pop();
						break;
				}
			}

			var result = _json.ToString();
			
			var realCount = result.Length > count ? count : result.Length;
			
			_json.Clear();
			if (realCount < result.Length) _json.Append(result.Substring(realCount));
			
			Array.Copy(result.ToCharArray(), 0, buffer, index, realCount);

			return realCount;
		}

		private void CheckAndInsertComma()
		{
			if (_states.Count == 0) return;

			if (_states.Peek() == State.Property)
				_states.Pop();
			else if (_states.Peek() != State.ArrayStart)
				_json.Append(",");

			switch (_states.Peek())
			{
				case State.ObjectStart:
					_states.Pop();
					_states.Push(State.Object);
					break;
				case State.ArrayStart:
					_states.Pop();
					_states.Push(State.Array);
					break;
			}
		}

		public override string ReadToEnd()
		{
			var result = new StringBuilder();
			var buffer = new char[1024];
			int count;
			
			while ((count = Read(buffer, 0, buffer.Length)) > 0)
			{
				result.Append(buffer, 0, count);
			}

			return result.ToString();
		}

		public override int ReadBlock(char[] buffer, int index, int count)
		{
			return Read(buffer, index, count);
		}

		public override int Read()
		{
			var buffer = new char[1];
			if (Read(buffer, 0, 1) == 0) return -1;
			return buffer[0];
		}

		public override int Peek()
		{
			throw new NotImplementedException();
		}

		public override string ReadLine()
		{
			throw new NotImplementedException();
		}

		public override void Close()
		{
			if (_closed) return;

			_reader.Close();
			_closed = true;
		}

		protected override void Dispose(bool disposing)
		{
			if (_disposed) return;

			if (disposing)
			{
				Close();
			}

			_disposed = true;
		}
	}
}