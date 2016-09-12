using System;

namespace WB.Core.SharedKernels.DataCollection
{
    /// <summary>
    /// Full identity of group or question: id and roster vector.
    /// </summary>
    /// <remarks>
    /// Is used only internally to simplify return of id and roster vector as return value
    /// and to reduce parameters count in calculation methods.
    /// Should not be made public or be used in any form in events or commands.
    /// </remarks>
    public class Identity
    {
        private int? hashCode = null;

        protected bool Equals(Identity other)
        {
            return this.Id.Equals(other.Id) && this.RosterVector.Identical(other.RosterVector);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                if (!this.hashCode.HasValue)
                {
                    int hc = this.RosterVector.Length;
                    for (int i = 0; i < this.RosterVector.Length; ++i)
                    {
                        hc = unchecked(hc*13 + this.RosterVector[i].GetHashCode());
                    }
                    this.hashCode = hc;
                }

                return this.hashCode.Value;
            }
        }

        public Guid Id { get; private set; }

        public RosterVector RosterVector { get; private set; }

        public Identity(Guid id, RosterVector rosterVector)
        {
            this.Id = id;
            this.RosterVector = rosterVector ?? RosterVector.Empty;
        }

        public override string ToString()
        {
            return ConversionHelper.ConvertIdentityToString(this);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((Identity) obj);
        }

        public bool Equals(Guid id, RosterVector rosterVector) => this.Equals(new Identity(id, rosterVector));

        public static bool operator ==(Identity a, Identity b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(Identity a, Identity b) => !(a == b);
    }
}