using System;
using System.Diagnostics;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities
{
    [DebuggerDisplay("Variable {Identity}. Value = {Value}")]
    public class InterviewTreeVariable : InterviewTreeLeafNode
    {
        public object Value { get; private set; }
        public bool HasValue => this.Value != null;

        public InterviewTreeVariable(Identity identity) : base(identity)
        {
        }

        public override string ToString()
        {
            var formattableString = $"Variable ({this.Identity}). Value = {Value ?? "'NULL'"}";
            
            return formattableString;
        }

        public void SetValue(object value) => this.Value = value;
        public override IInterviewTreeNode Clone()
        {
            return (IInterviewTreeNode)this.MemberwiseClone();
        }
    }
}