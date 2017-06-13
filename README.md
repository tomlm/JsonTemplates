# Json Templates
Simple Json transformation tool to bind Json data objects to a complex Json structure.

# Binding
The template is expressed in terms of a JSON object where the values are bound using JPath (http://goessner.net/articles/JsonPath/). 

**Value Binding** 

A property value with **{=Path}** will use JPath to bind to a property and output the native value of the property

**String Binding**

A property with **{ Path }** will use JPath to bind to a property and output the *string* value of the property.

You can specify multiple string bindings in one string to make *compound string*.

> Example:
>
> "Your first name is: *{ firstName }* and last name is: *{ lastName }*"
> 
> Becomes
> 
> "Your first name is: Tom and last name is: Laird-McConnell"

You can control the string that is generated using the **Format(path, formatCode)** function, 
where the formatCode is the .NET format string for .ToString(formatCode).  

> Example:
> 
> "Date your document created:{ format(path,D) }"
> 
> Becomes:
> 
> "Date your document created: Thursday, January 12, 2017"
> 


**Array Item Binding**

You can bind to an array of items and define a template that applies to each element.  You do this by specifying the JPath in the
property name like so:

Example:

    "items={path}" : [
        {
            "prop":"{=someValue}" 
        }
    ]
   
## Usage
    JsonTemplate template = new JsonTemplate(jsonTemplate);
    dynamic result = template.Bind(source);

> You can call template.Bind() as much as you want and it is thread safe.  


#### Example Data:

    {
      "aStr": "A String",
      "aNumber": 13,
      "aDatetime": "2017-01-12T08:28:48.0473306-08:00",
      "aBoolean": true,
      "aObject": {
        "X": "XValue",
        "Y": 99
      },
      "foo": {
        "bar": [
          {
            "Name": "Item1",
            "Rating": 1,
            "subObject": {
              "x": 11
            }
          },
          {
            "Name": "Item2",
            "Rating": 2,
            "subObject": {
              "x": 22
            }
          },
          {
            "Name": "Item3",
            "Rating": 3,
            "subObject": {
              "x": 33
            }
          }
        ]
      }
    }

#### Example Template:

    {
        "stringBinding": "{=aStr}",
        "compoundBinding": "This is a {aStr} an so is {foo.bar[2].Name}!",
        "joinSimple": "This is a {foo.bar[*].Name}!",
        "boolStringBinding": "{aBoolean}",
        "boolBinding": "{=aBoolean}",
        "numberStringBinding": "{aNumber}",
        "numberBinding": "{=aNnumber}",
        "noBinding": "This should pass through unchanged",
        "dateBinding": "{=aDateTime}",
        "dateStringBinding": "{aDateTime}",
        "formatBinding": "{format(aDateTime,D)}",
        "subObjectBinding": {
            "title": "{foo.bar[1].Name}"
        },
        "simpleArray": "{=foo.bar[*].Name}",
        "myArray={foo.bar}": [
            {
                "num": "{Rating}",
                "metadata": {
                    "title": "{Name}: {subObject.x}"
                }
            }
        ]
    }


#### Example output

    {
        "stringBinding": "A String",
        "compoundBinding": "This is a A String an so is Item3!",
        "joinSimple": "This is a Item1, Item2, Item3!",
        "boolStringBinding": "True",
        "boolBinding": true,
        "numberStringBinding": "13",
        "numberBinding": 13,
        "noBinding": "This should pass through unchanged",
        "dateBinding": "2017-01-12T08:28:48.0473306-08:00",
        "dateStringBinding": "1/12/2017 8:28:48 AM",
        "formatBinding": "Thursday, January 12, 2017",
        "subObjectBinding": {
            "title": "Item2"
        },
        "simpleArray": [
            "Item1",
            "Item2",
            "Item3"
        ],
        "myArray": [
            {
                "num": "1",
                "metadata": {
                    "title": "Item1: 11"
                }
            },
            {
                "num": "2",
                "metadata": {
                    "title": "Item2: 22"
                }
            },
            {
                "num": "3",
                "metadata": {
                    "title": "Item3: 33"
                }
            }
        ]
    }