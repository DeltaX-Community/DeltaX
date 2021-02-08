namespace DeltaX.RealTime.RtExpression
{
    using org.mariuszgromada.math.mxparser;
    using DeltaX.RealTime.Interfaces;
    using System;
    using System.Text.RegularExpressions;
    using System.Linq; 

    public class RtTagExpression : RtTagBase
    {
        private IRtTag[] ArgumentsTags;
        private Expression expression;

        public RtTagExpression(IRtConnector creator, string expresionFull, IRtTagOptions options = null)
        {
            string patternAddressRegex = @"\{[^{} ]+\}";
            string expresion = expresionFull.Trim().TrimStart('=');

            Regex rgx = new Regex(patternAddressRegex, RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(expresionFull);

            if (matches.Count > 0)
            {
                int i = 0;
                var argumentsTags = new IRtTag[matches.Count];
                foreach (Match match in matches)
                {
                    var tagDefinition = match.Value.Trim().TrimStart('{').TrimEnd('}');
                    argumentsTags[i] = creator.AddTagDefinition(tagDefinition, options);
                    expresion = expresion.Replace(match.Value, $"arg{i}");
                    i++;
                }
                Initialize(expresion, argumentsTags);
            }
            else
            {
                Initialize(expresion, null);
            }
        }

        public RtTagExpression(string expresionString, IRtTag[] argumentsTags)
        {
            Initialize(expresionString, argumentsTags);
        }

        private void Initialize(string expresionString, IRtTag[] argumentsTags)
        {
            ArgumentsTags = argumentsTags ?? new IRtTag[] { };

            try
            {
                expression = new Expression(expresionString.Trim());

                var arguments = ArgumentsTags.Select((a, i) => new Argument($"arg{i}", 0)).ToArray();
                expression.addArguments(arguments);

                TagName = expresionString;
                Topic = GetExpresionRepr();

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
                atag.ValueUpdated += TagArgumentOnUpdatedValue;
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
            if (e.Updated > base.Updated)
            {
                Eval();
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

        private string GetExpresionRepr()
        {
            string expFull = expression.getExpressionString();

            foreach (var e in ArgumentsTags.Select((tag, idx) => (tag.TagName, idx)))
            {
                expFull = expFull.Replace($"arg{e.idx}", $"{{{e.TagName}}}");
            }

            return expFull;
        }

        public IRtValue Eval()
        {
            try
            {
                DateTime _updated = base.Updated;
                bool st = true;
                foreach (var e in ArgumentsTags.Select((tag, index) => (tag, index)))
                {
                    if (e.tag.Status == false || double.IsNaN(e.tag.Value.Numeric))
                    {
                        st = false;
                    }

                    expression.getArgument(e.index).setArgumentValue(e.tag.Value.Numeric);

                    _updated = e.tag.Updated > _updated ? e.tag.Updated : _updated;
                }

                var value = expression.calculate();
                RaiseOnUpdatedValue(this, RtValue.Create(value), _updated, st);
                return base.Value;
            }
            catch
            {
                RaiseOnUpdatedValue(this, RtValue.Create(double.NaN), DateTime.Now, false);
                return base.Value;
            }
        }

        public IRtTag GetArg(int argIdx)
        {
            return ArgumentsTags[argIdx];
        }

        public string GetExpresionValues()
        {
            string expFull = expression.getExpressionString();

            foreach (var e in ArgumentsTags.Select((tag, idx) => (tag.Value, idx)))
            {
                expFull = expFull.Replace($"arg{e.idx}", $"{{{e.Value.Text}}}");
            }

            return expFull;
        }

        public override IRtValue Value
        {
            get
            {
                if (HasNewValue())
                {
                    Eval();
                }
                return base.Value;
            }
        }

        public override bool Status
        {
            get
            {
                if (HasNewValue())
                {
                    Eval();
                }
                return base._status;
            }
        }

        public override DateTime Updated
        {
            get
            {
                if (HasNewValue())
                {
                    Eval();
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
    }
}
