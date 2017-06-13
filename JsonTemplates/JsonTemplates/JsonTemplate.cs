using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonTemplates
{
    /// <summary>
    /// Class which applies a Json defined template to transfroms
    /// NOTES: 
    /// Template needs to have root property called "template"
    /// Additional templates can be added by simply adding peer nodes to "template"
    ///     {path} will data bind to a value as a string, you can use multiple string bindings to build a compound string
    ///     {=path} will data bind to a value as it's native type.  If it is a number it will be a number, a bool a bool etc. 
    ///     {format(path, format)} will process path and then use String.Format("format") to format the output. 
    ///     {array(path, itemTemplateName)} will process a collection of elements and bind them to the itemTemplateName
    /// </summary>
    public class JsonTemplate
    {
        private const string formatPrefix = "format(";
        private const string arrayPrefix = "array(";
        private JObject template;

        /// <summary>
        /// Create template from json string
        /// </summary>
        /// <param name="template"></param>
        public JsonTemplate(string template)
            : this((JObject)JsonConvert.DeserializeObject(template))
        {
        }

        /// <summary>
        /// Create template from already loaded json
        /// </summary>
        /// <param name="template"></param>
        public JsonTemplate(JObject template)
        {
            this.template = template;
            if (this.template["template"] == null)
                throw new MissingFieldException("There is no root property called 'template' defined.");
        }

        /// <summary>
        /// Bind source object to the internal template and output a JObject 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="name">Name to transform (default is </param>
        /// <returns></returns>
        public JObject Bind(object source)
        {
            return TransformToView((source is JObject) ? (JObject)source : JObject.FromObject(source));
        }

        /// <summary>
        /// Bind source object to the internal template and output as typed object
        /// </summary>
        /// <typeparam name="OutputT"></typeparam>
        /// <param name="source"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public OutputT Bind<OutputT>(object source)
        {
            var result = TransformToView((source is JObject) ? (JObject)source : JObject.FromObject(source));
            return result.ToObject<OutputT>();
        }

        protected JObject TransformToView(JObject source, string name = "template")
        {
            JObject view = (JObject)this.template[name];
            if (view == null)
                throw new ArgumentNullException($"There is is no template named {name}");
            return TransformToView(source, view);
        }

        private JObject TransformToView(JObject source, JObject view)
        {
            JObject target = new JObject();
            foreach (var property in view)
            {
                switch (property.Value.Type)
                {
                    case JTokenType.String:
                        target[property.Key] = ProcessBinding(property.Value.ToString(), source);
                        break;

                    case JTokenType.Object:
                        target[property.Key] = TransformToView(source, (JObject)property.Value);
                        break;

                    case JTokenType.Array:
                        JArray arr = new JArray();
                        foreach (var item in property.Value)
                        {
                            arr.Add(TransformToView(source, (JObject)item));
                        }
                        target[property.Key] = arr;
                        break;

                    default:
                        target[property.Key] = property.Value;
                        break;
                }
            }
            return target;
        }

        // private static Regex inlineBinding = new Regex("({[^{]+?})", RegexOptions.Compiled);

        protected dynamic ProcessBinding(string binding, JObject source)
        {
            // process string binding
            if (binding.Length > 2)
            {
                // value binding
                if (binding.StartsWith("{=") && binding.EndsWith("}"))
                {
                    // simple binding, this can return number and date
                    binding = binding.Trim('{', '}');
                    var tokens = source.SelectTokens(binding.TrimStart('='));
                    if (tokens.Count() == 1)
                        // bind to simple property value
                        return tokens.FirstOrDefault();
                    else
                        // return as array property
                        return new JArray(tokens.ToArray());
                }
                // handle array() command
                else if (binding.ToLower().StartsWith("{" + arrayPrefix) && binding.EndsWith(")}"))
                {
                    return ProcessArrayFunction(source, binding);
                }
                else
                {
                    // handle string binding, this handles compound bindings in a string
                    var inlineBindings = FindInlineBindings(binding);
                    if (inlineBindings.Any())
                    {
                        // compound string builder
                        StringBuilder sb = new StringBuilder();
                        int start = 0;
                        foreach (Binding inlineBinding in inlineBindings)
                        {
                            sb.Append(binding.Substring(start, inlineBinding.Index - start));
                            var subBinding = inlineBinding.Value.Trim('{', '}');
                            if (subBinding.StartsWith(formatPrefix) && subBinding.EndsWith(")"))
                            {
                                // handle format () command
                                sb.Append(ProcessFormBinding(source, subBinding));
                            }
                            else
                            {
                                var results = source.SelectTokens(inlineBinding.Value.Trim('{', '}'));
                                var tokens = new List<JToken>();
                                foreach (var result in results)
                                {
                                    if (result.Type == JTokenType.Array)
                                        tokens.AddRange(result);
                                    else
                                        tokens.Add(result);
                                }
                                if (tokens.Count() > 1)
                                    sb.Append(JoinCollection(tokens));
                                else
                                    sb.Append(tokens.First().ToString());
                            }
                            start = inlineBinding.Index + inlineBinding.Value.Length;
                        }
                        sb.Append(binding.Substring(start));
                        return sb.ToString();
                    }
                }
            }
            return binding.Replace("\\{", "{").Replace("\\}", "}");
        }

        /// <summary>
        /// Format(path, formatstring)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="subBinding"></param>
        /// <returns></returns>
        private static string ProcessFormBinding(JObject source, string subBinding)
        {
            var path = subBinding.Substring(formatPrefix.Length, subBinding.Length - 1 - formatPrefix.Length);
            int iStart = path.LastIndexOf(",");
            if (iStart > 0)
            {
                var command = path;
                path = path.Substring(0, iStart);
                string format = command.Substring(iStart + 1).Trim(' ');
                var token = source.SelectToken(path);
                return String.Format($"{{0:{format}}}", token);
            }
            throw new ArgumentNullException("Missing 2nd argument to format. Example: {{format(path,D)}}");
        }

        private static string JoinCollection(IEnumerable<JToken> tokens)
        {
            string seperator = ", ";
            string lastSep = ", ";
            List<JToken> values = new List<JToken>();
            foreach (var token in tokens)
            {
                if (token.Type == JTokenType.Array)
                    values.AddRange(token.ToArray());
                else
                    values.Add(token);
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < values.Count(); i++)
            {
                if (i > 0 && i < values.Count() - 1)
                    sb.Append(seperator);
                if (values.Count() > 1 && i == values.Count() - 1)
                    sb.Append(lastSep);
                sb.Append(values[i].ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// handle array(templateName, path)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="binding"></param>
        /// <returns></returns>
        protected JArray ProcessArrayFunction(JObject source, string binding)
        {
            string[] args = binding.Trim('{', '}', ')').Substring(arrayPrefix.Length).Split(',');
            var path = args[0].Trim();
            var templateName = args[1].Trim();
            var arr = source.SelectToken(path);

            JArray target = new JArray();
            foreach (var item in arr)
            {
                target.Add(TransformToView((JObject)item, (JObject)this.template[templateName]));
            }
            return target;
        }

        protected IList<Binding> FindInlineBindings(string path)
        {
            List<Binding> bindings = new List<Binding>();
            int iStart = path.IndexOf('{');
            while (iStart >= 0)
            {
                int iEnd = path.IndexOf('}', iStart);
                if (iEnd >= 0)
                {
                    bindings.Add(new Binding(iStart, path.Substring(iStart, iEnd - iStart + 1)));
                    iStart = path.IndexOf('{', iEnd + 1);
                }
                else
                    break;
            }
            return bindings;
        }

    }

    public class Binding
    {
        public Binding(int index, string val)
        {
            Index = index;
            Value = val;
        }

        public int Index { get; set; }

        public string Value { get; set; }
    }
}