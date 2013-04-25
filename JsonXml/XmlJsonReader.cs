using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;
using Newtonsoft.Json;

namespace JsonXml
{
	public class XmlJsonReader : JsonReader
	{
		private class Token
		{
			public JsonToken Type;
			public object Value;

			public static Token GetNull()
			{
				return new Token {Type = JsonToken.Null};
			}

			public static Token GetPropertyName(string name)
			{
				return new Token {Type = JsonToken.PropertyName, Value = name};
			}

			public static Token GetAttrName(string name)
			{
				return new Token {Type = JsonToken.PropertyName, Value = string.Format("@{0}", name)};
			}

			public static Token GetValue(string value)
			{
				return new Token { Type = Utils.GetValueType(value), Value = Utils.ParseValue(value) };
			}

			public static Token GetStartObject()
			{
				return new Token {Type = JsonToken.StartObject};
			}

			public static Token GetEndObject()
			{
				return new Token { Type = JsonToken.EndObject };
			}

			public static Token GetStartArray()
			{
				return new Token { Type = JsonToken.StartArray };
			}

			public static Token GetEndArray()
			{
				return new Token { Type = JsonToken.EndArray };
			}
		}

		private enum StateType
		{
			Object,
			Start,
			Property,
			Finished,
			Array
		}

		private new class State
		{
			public StateType Type;
			public string Name;

			public static State Start()
			{
				return new State {Type = StateType.Start};
			}

			public static State Property(string name)
			{
				return new State {Type = StateType.Property, Name = name};
			}
		}

		private readonly XmlReader _xmlReader;
		private object _value;
		private JsonToken _tokenType;
		private readonly Queue<Token> _tokens;
		private readonly Stack<State> _states;
		private bool _disposed;
		private bool _closed;
		private readonly Queue<string> _rootPathChunks;
		private int _level;
		private readonly Stack<string> _matchedPathChunks;

		private readonly Dictionary<string, List<string>> _processingInstructions;
		private bool _readExecuted;

		public XmlJsonReader(XmlReader xmlReader) : this(xmlReader, null) {}

		public XmlJsonReader(XmlReader xmlReader, string rootPath)
		{
			_closed = false;
			_disposed = false;
			_xmlReader = xmlReader;
			_value = null;
			_tokenType = JsonToken.None;
			_level = 0;

			_processingInstructions = new Dictionary<string, List<string>>();

			var rootPathChunks = rootPath == null ? new string[0] : rootPath.Trim("/".ToCharArray()).Split("/".ToCharArray());
			_rootPathChunks = new Queue<string>(rootPathChunks);
			_matchedPathChunks = new Stack<string>();

			_tokens = new Queue<Token>();
			_states = new Stack<State>();

			_states.Push(State.Start());
			_tokens.Enqueue(Token.GetStartObject());

			_readExecuted = false;
		}

		public void ReadProcessingInstructions()
		{
			if (!_readExecuted)
				while(_tokens.Count < 2 && _xmlReader.Read())
					ParseCurrentNode();
		}

		public override bool Read()
		{
			_readExecuted = true;

			ReadNext();

			if (_tokens.Count > 0)
			{
				var token = _tokens.Dequeue();
				_value = token.Value;
				_tokenType = token.Type;

				return true;
			}

			if (_states.Count == 1 && _states.Peek().Type == StateType.Start)
			{
				_states.Peek().Type = StateType.Finished;
				_tokenType = JsonToken.EndObject;
				_value = null;

				return true;
			}

			return false;
		}

		private void ReadNext()
		{
			while (_tokens.Count == 0 && _xmlReader.Read())
				ParseCurrentNode();
		}

		private void ParseCurrentNode()
		{
			switch (_xmlReader.NodeType)
			{
				case XmlNodeType.ProcessingInstruction:
					if (!ProcessingInstructions.ContainsKey(_xmlReader.LocalName))
						ProcessingInstructions[_xmlReader.LocalName] = new List<string>();
					
					ProcessingInstructions[_xmlReader.LocalName].Add(_xmlReader.Value);
					break;

				case XmlNodeType.Element:
					_level++;

					if (_rootPathChunks.Count > 0)
					{
						var topChunk = _rootPathChunks.Peek();

						if (topChunk == "*" || (topChunk == _xmlReader.LocalName && _matchedPathChunks.Count == _level - 1))
						{
							var chunks = _rootPathChunks.Dequeue();
							_matchedPathChunks.Push(chunks);
						}

						break;
					}

					if (_states.Peek().Type == StateType.Property)
					{
						if (Utils.Pluralize(_xmlReader.LocalName) == _states.Peek().Name)
						{
							_states.Peek().Type = StateType.Array;
							_tokens.Enqueue(Token.GetStartArray());
						}
						else
						{
							_states.Peek().Type = StateType.Object;
							_tokens.Enqueue(Token.GetStartObject());
						}
					}

					if (_states.Peek().Type != StateType.Array)
					{
						_tokens.Enqueue(Token.GetPropertyName(_xmlReader.LocalName));
					}

					_states.Push(State.Property(_xmlReader.LocalName));

					var hasAttrs = _xmlReader.MoveToFirstAttribute();

					if (hasAttrs)
					{
						_states.Peek().Type = StateType.Object;
						_tokens.Enqueue(Token.GetStartObject());
					}

					while (hasAttrs)
					{
						_tokens.Enqueue(Token.GetAttrName(_xmlReader.LocalName));
						_xmlReader.ReadAttributeValue();
						_tokens.Enqueue(Token.GetValue(_xmlReader.Value));
						hasAttrs = _xmlReader.MoveToNextAttribute();
					}

					_xmlReader.MoveToElement();

					if (_xmlReader.IsEmptyElement)
					{
						_tokens.Enqueue(_states.Peek().Type == StateType.Property ? Token.GetNull() : Token.GetEndObject());
						_states.Pop();
					}

					break;

				case XmlNodeType.Text:
					switch (_states.Peek().Type)
					{
						case StateType.Property:
							_tokens.Enqueue(Token.GetValue(_xmlReader.Value));
							break;

						case StateType.Object:
							_tokens.Enqueue(Token.GetPropertyName("#text"));
							_tokens.Enqueue(Token.GetValue(_xmlReader.Value));
							break;
					}
					break;

				case XmlNodeType.EndElement:
					_level--;

					switch (_states.Peek().Type)
					{
						case StateType.Object:
							_tokens.Enqueue(Token.GetEndObject());
							_states.Pop();
							break;

						case StateType.Property:
							_states.Pop();
							break;

						case StateType.Array:
							_tokens.Enqueue(Token.GetEndArray());
							_states.Pop();
							break;
					}
					break;
			}
		}

		public override object Value
		{
			get { return _value; }
		}

		public override JsonToken TokenType
		{
			get { return _tokenType; }
		}

		public Dictionary<string, List<string>> ProcessingInstructions
		{
			get { return _processingInstructions; }
		}

		public override void Close()
		{
			if (_closed) return;
			
			_xmlReader.Close();
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

		#region NotImplemented
		public override int? ReadAsInt32()
		{
			throw new NotImplementedException();
		}

		public override string ReadAsString()
		{
			throw new NotImplementedException();
		}

		public override byte[] ReadAsBytes()
		{
			throw new NotImplementedException();
		}

		public override decimal? ReadAsDecimal()
		{
			throw new NotImplementedException();
		}

		public override DateTime? ReadAsDateTime()
		{
			throw new NotImplementedException();
		}

		public override DateTimeOffset? ReadAsDateTimeOffset()
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}