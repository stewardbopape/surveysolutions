﻿using System;

namespace WB.Core.BoundedContexts.Designer.Services
{
    public class ExpressionLocation : MarshalByRefObject
    {
        public Guid Id { set; get; }

        public ItemType ItemType { set; get; }

        public ExpressionType ExpressionType { set; get; }

        public ExpressionLocation()
        {
        }

        public ExpressionLocation(string stringValue)
        {
            var expressionLocation = stringValue.Split(':');
            if(expressionLocation.Length != 3)
                throw new ArgumentException("stringValue");

            this.ItemType = (ItemType) Enum.Parse(typeof (ItemType), expressionLocation[0], true);
            this.ExpressionType = (ExpressionType)Enum.Parse(typeof(ExpressionType), expressionLocation[1], true);
            this.Id = Guid.Parse(expressionLocation[2]);
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}:{2}", ItemType, ExpressionType, Id);
        }
    }
}
