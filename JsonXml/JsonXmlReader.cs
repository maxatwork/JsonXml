using System;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;

namespace JsonXml
{
	public class JsonXmlReader : XmlReader
	{
		private readonly JsonReader _jsonReader;
		private ReadState _readState;

		private XmlNodeType _nodeType;
		private bool _isEmptyElement;
		private string _localName;
		private string _value;

		private string _elementLocalName;

		private readonly Queue<Node> _nodes;
		private readonly Stack<State> _states;
		private bool _disposed;
		private bool _closed;

		private readonly Queue<string> _rootPathChunks;
		private int _depth;
		private bool _eof;
		private readonly Stack<string> _rootPathMatchedChunks;

		public JsonXmlReader(JsonReader reader, string rootPath = null)
		{
			_closed = false;
			_disposed = false;
			_jsonReader = reader;
			_readState = ReadState.Initial;
			_nodeType = XmlNodeType.None;
			_isEmptyElement = false;
			_localName = null;
			_value = null;
			_depth = 0;
			_eof = false;

			_nodes = new Queue<Node>();
			_states = new Stack<State>();

			var rootPathChunks = rootPath == null ? new string[0] : rootPath.Trim("/".ToCharArray()).Split("/".ToCharArray());
			_rootPathChunks = new Queue<string>(rootPathChunks);
			_rootPathMatchedChunks = new Stack<string>();
		}

		private enum StateType
		{
			Property,
			Object,
			Array,
			Attribute
		}

		private class State
		{
			public string Name;
			public StateType Type;
		}

		private class Node
		{
			public XmlNodeType Type;
			public string LocalName;
			public string Value;
			public bool IsEmptyElement;
		}

		public override bool MoveToFirstAttribute()
		{
			_elementLocalName = _localName;
			return MoveToNextAttribute();
		}

		public override bool MoveToNextAttribute()
		{
			ReadNextNode();
			
			if (_nodes.Count > 0 && _nodes.Peek().Type == XmlNodeType.Attribute)
			{
				var node = _nodes.Dequeue();
				_localName = node.LocalName;
				_nodeType = node.Type;
				_value = node.Value;

				return true;
			}

			return false;
		}

		public override bool MoveToElement()
		{
			_localName = _elementLocalName;
			_nodeType = XmlNodeType.Element;
			return true;
		}

		public override bool ReadAttributeValue()
		{
			if (_nodes.Count == 0)
				return false;

			var node = _nodes.Dequeue();
			_value = node.Value;
			_nodeType = node.Type;

			return true;
		}

		public override bool Read()
		{
			while (_rootPathChunks.Count > 0)
			{
				var topChunk = _rootPathChunks.Dequeue();
				_nodes.Enqueue(new Node
				               	{
				               		IsEmptyElement = false,
									LocalName = topChunk,
									Type = XmlNodeType.Element,
									Value = null
				               	});
				_rootPathMatchedChunks.Push(topChunk);
			}
			
			ReadNextNode();

			var result = TryDequeueNode();

			if (result)
			{
				_eof = false;
				return true;
			}

			while (_rootPathMatchedChunks.Count > 0)
			{
				var topChunk = _rootPathMatchedChunks.Pop();
				_nodes.Enqueue(new Node
				{
					IsEmptyElement = false,
					LocalName = topChunk,
					Type = XmlNodeType.EndElement,
					Value = null
				});
			}

			result = TryDequeueNode();

			_eof = !result;
			return result;
		}

		private bool TryDequeueNode()
		{
			bool result;
			if (_nodes.Count > 0)
			{
				var node = _nodes.Dequeue();
				_localName = node.LocalName;
				_nodeType = node.Type;
				_value = node.Value;
				_isEmptyElement = node.IsEmptyElement;
				_depth = _states.Count;
				_eof = false;

				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}

		private void ReadNextNode()
		{
			while (_nodes.Count == 0 && _jsonReader.Read())
			{
				State state;
				Node node;
				string name;

				switch (_jsonReader.TokenType)
				{
					case JsonToken.StartObject:
						if (_states.Count > 0)
						{
							state = _states.Peek();
							name = Utils.Singularize(state.Name);

							_nodes.Enqueue(new Node
							               	{
							               		IsEmptyElement = false,
							               		LocalName = name,
							               		Type = XmlNodeType.Element
							               	});

							_states.Push(new State {Name = name, Type = StateType.Object});
						}
						break;
					
					case JsonToken.Integer:
					case JsonToken.Boolean:
					case JsonToken.Float:
					case JsonToken.String:
						state = _states.Peek();
						_nodes.Enqueue(new Node
						               	{
						               		IsEmptyElement = false, 
											LocalName = Utils.Singularize(state.Name), 
											Type = XmlNodeType.Element
						               	});

						_nodes.Enqueue(new Node {Type = XmlNodeType.Text, Value = Utils.UnparseValue(_jsonReader.Value)});
						
						_nodes.Enqueue(new Node
						               	{
						               		IsEmptyElement = false, 
											LocalName = Utils.Singularize(state.Name), 
											Type = XmlNodeType.EndElement
						               	});
						break;

					case JsonToken.EndArray:
					case JsonToken.EndObject:
						if (_states.Count > 0)
						{
							state = _states.Pop();
							node = new Node
							           	{
							           		Type = XmlNodeType.EndElement,
							           		LocalName = state.Name
							           	};
							_nodes.Enqueue(node);
						}
						break;

					case JsonToken.PropertyName:
						name = _jsonReader.Value.ToString();

						node = new Node
						           	{
						           		Type = name.StartsWith("@") ? XmlNodeType.Attribute : XmlNodeType.Element,
						           		LocalName = name.StartsWith("@") ? name.Substring(1) : name
						           	};

						_states.Push(new State
						             	{
						             		Name = node.LocalName, 
											Type = node.Type == XmlNodeType.Attribute ? StateType.Attribute : StateType.Property
						             	});

						_jsonReader.Read();
						node.IsEmptyElement = _jsonReader.TokenType == JsonToken.Null;
						_nodes.Enqueue(node);

						if (node.IsEmptyElement)
						{
							_states.Pop();
						}

						switch (_jsonReader.TokenType)
						{
							case JsonToken.Integer:
							case JsonToken.Boolean:
							case JsonToken.Float:
							case JsonToken.String:
								state = _states.Pop();
								node = new Node
								       	{
								       		Type = XmlNodeType.Text,
								       		Value = Utils.UnparseValue(_jsonReader.Value)
								       	};
								_nodes.Enqueue(node);
								
								if (state.Type == StateType.Property) 
									_nodes.Enqueue(new Node {Type = XmlNodeType.EndElement, LocalName = state.Name});
								
								break;

							case JsonToken.StartObject:
								_states.Peek().Type = StateType.Object;
								break;

							case JsonToken.StartArray:
								_states.Peek().Type = StateType.Array;
								break;
						}
						break;
				}
			}
		}
		
		public override void Close()
		{
			if (_closed) return;

			_readState = ReadState.Closed;
			_jsonReader.Close();
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

		public override XmlNodeType NodeType
		{
			get { return _nodeType; }
		}

		public override string LocalName
		{
			get { return _localName; }
		}

		public override string NamespaceURI
		{
			get { return string.Empty; }
		}

		public override string Prefix
		{
			get { return string.Empty; }
		}

		public override string Value
		{
			get { return _value; }
		}

		public override string BaseURI
		{
			get { return "JSON"; }
		}

		public override bool IsEmptyElement
		{
			get { return _isEmptyElement; }
		}

		public override ReadState ReadState { get { return _readState; } }

		public override bool EOF
		{
			get { return _eof; }
		}

		public override int Depth
		{
			get { return _depth; }
		}

		#region NotImplementedFeatures
		public override string GetAttribute(string name)
		{
			throw new NotImplementedException();
		}

		public override string GetAttribute(string name, string namespaceURI)
		{
			throw new NotImplementedException();
		}

		public override string GetAttribute(int i)
		{
			throw new NotImplementedException();
		}

		public override bool MoveToAttribute(string name)
		{
			throw new NotImplementedException();
		}

		public override bool MoveToAttribute(string name, string ns)
		{
			throw new NotImplementedException();
		}

		public override string LookupNamespace(string prefix)
		{
			throw new NotImplementedException();
		}

		public override void ResolveEntity()
		{
			throw new NotImplementedException();
		}

		public override int AttributeCount
		{
			get { throw new NotImplementedException(); }
		}

		public override XmlNameTable NameTable
		{
			get { throw new NotImplementedException(); }
		}
		#endregion
	}
}