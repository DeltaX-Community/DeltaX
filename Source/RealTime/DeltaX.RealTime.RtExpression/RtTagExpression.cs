namespace DeltaX.RealTime.RtExpression
{
    using org.mariuszgromada.math.mxparser;
    using DeltaX.RealTime.Interfaces;
    using System;
    using System.Text.RegularExpressions;
    using System.Linq; 

    public class RtTagExpression : RtTagBase, IRtTag
    {
        private IRtTag[] ArgumentsTags;
        private Expression expression;
        private static string patternAddressRegex = @"\{[^{} ]+\}";
        private static Regex rgx = new Regex(patternAddressRegex, RegexOptions.IgnoreCase);

        public static IRtTag AddExpression(IRtConnector creator, string expresionFull, IRtTagOptions options = null)
        {  
            MatchCollection matches = rgx.Matches(expresionFull); 
            if (matches.Count > 0)
            {
                string expresion = expresionFull;
                int i = 0;
                var argumentsTags = new IRtTag[matches.Count];
                foreach (Match match in matches)
                {
                    var tagDefinition = match.Value.Trim().TrimStart('{').TrimEnd('}');
                    argumentsTags[i] = creator.AddTagDefinition(tagDefinition, options);
                    expresion = expresion.Replace(match.Value, $"arg{i}");
                    i++;
                }
                return new RtTagExpression(expresionFull, expresion, argumentsTags);
            }
            else if (expresionFull.StartsWith("="))
            {
                return new RtTagExpression(expresionFull, expresionFull, null);
            }
            else
            {
                return creator.AddTagDefinition(expresionFull, options); 
            }
        }

        public static bool IsExpression(string expresionFull)
        {
            return rgx.IsMatch(expresionFull) || expresionFull.StartsWith("=");
        }

        public RtTagExpression(string tagName, string expresionString, IRtTag[] argumentsTags)
        {
            TagName = tagName;
            Initialize(expresionString.TrimStart('='), argumentsTags);
        }

        private void Initialize(string expresionString, IRtTag[] argumentsTags)
        {
            ArgumentsTags = argumentsTags ?? new IRtTag[] { };

            try
            {
                expression = new Expression(expresionString.Trim());

                var arguments = ArgumentsTags.Select((a, i) => new Argument($"arg{i}", 0)).ToArray();
                expression.addArguments(arguments);

                Topic = GetExpresionTopic();

                Eval();
                AttachEventsTagArguments();
            }
            catch (Exception e)
            {
                throw new Exception("Initialize RtTagExpression error", e);
            }
        }

        private void AttachEventsTagArguments()
        {
            foreach (var atag in ArgumentsTags)
            {
                // atag.ValueUpdated -= TagArgumentOnUpdatedValue; 
                // atag.ValueUpdated += TagArgumentOnUpdatedValue; 
            }
        }
         

        private void DettachEventsTagArguments()
        {
            foreach (var atag in ArgumentsTags)
            {
                atag.ValueUpdated -= TagArgumentOnUpdatedValue; 
            }
        }

        private void TagArgumentOnUpdatedValue(object sender, IRtTag e)
        {
            lock (this)
            {
                if (HasNewValue() || HasNewStatus())
                {
                    Eval();
                }
            }
        }

        private bool HasNewValue()
        {
            var tag = ArgumentsTags.OrderByDescending(t => t.Updated).FirstOrDefault();
            if (tag != null && tag.Updated > base.Updated)
            {
                return true;
            }
            return false;
        }

        private bool HasNewStatus()
        {
            return _status != GetStatus();
        }

        private bool GetStatus()
        {
            return ArgumentsTags.Count() == 0 || ArgumentsTags.All(t => t.Status);
        }

        public IRtValue Eval()
        {
            lock (this)
            {
                try
                {
                    DateTime? _updated = null;
                    foreach (var e in ArgumentsTags.Select((tag, index) => (tag, index)))
                    {
                        expression.getArgument(e.index).setArgumentValue(e.tag.Value.Numeric);
                        _updated = !_updated.HasValue || e.tag.Updated > _updated ? e.tag.Updated : _updated;
                    }

                    var value = expression.calculate();
                    RaiseOnUpdatedValue(this, RtValue.Create(value), _updated ?? DateTime.Now, GetStatus());
                    return base.Value;
                }
                catch (Exception e)
                {
                    var msg = e.Message;
                    RaiseOnUpdatedValue(this, RtValue.Create(double.NaN), DateTime.Now, false);
                    return base.Value;
                }
            }
        }

        public IRtTag GetArg(int argIdx)
        {
            return ArgumentsTags[argIdx];
        }

        public string GetExpresionFull()
        {
            lock (this)
            {
                string expFull = expression.getExpressionString();

                foreach (var e in ArgumentsTags.Select((tag, idx) => (tag, idx)))
                {
                    expFull = expFull.Replace($"arg{e.idx}", $"{{{e.tag}}}");
                }

                return expFull;
            }
        }


        private string GetExpresionTopic()
        {
            lock (this)
            {
                string expFull = expression.getExpressionString();

                foreach (var e in ArgumentsTags.Select((tag, idx) => (tag.Topic, idx)))
                {
                    expFull = expFull.Replace($"arg{e.idx}", $"{{{e.Topic}}}");
                }

                return expFull;
            }
        }

        public override IRtValue Value
        {
            get
            {
                lock (this)
                {
                    if (HasNewValue() || HasNewStatus())
                    {
                        Eval();
                    }
                }
                return base.Value;
            }
        }

        public override bool Status
        {
            get
            {
                lock (this)
                {
                    if (HasNewValue() || HasNewStatus())
                    {
                        Eval();
                    }
                }
                return _status;
            }
        }

        public override DateTime Updated
        {
            get
            {
                lock (this)
                {
                    if (HasNewValue() || HasNewStatus())
                    {
                        Eval();
                    }
                }
                return base.Updated;
            }
        }

        public override bool Set(IRtValue value)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            DettachEventsTagArguments();
            base.Dispose();
            ArgumentsTags = null;
            expression = null;
        }

        public override string ToString()
        {
            return GetExpresionFull();
        }
    }
}
