#JsonXml

Sequential JSON <--> XML conversion for .NET

Provides 
 - JsonXmlReader -- implementation of XmlReader which reads JSON from provided JsonReader and converts it to XML
 - XmlJsonReader -- implementation of JsonReader which reads XML from provided XmlReader and converts it to JSON

##Attributes

```xml
<Root>
  <Foo bar="baz" />
</Root>
```

```json
{
  "Foo" : {
    "@bar": "baz"
  }
}
```

##Arrays

```xml
<Root>
  <Items>
    <Item>foo</Item>
    <Item>bar</Item>
    <Item>baz</Item>
  </Items>
</Root>
```

```json
{
  "Items": [ "foo", "bar", "baz" ]
}
```