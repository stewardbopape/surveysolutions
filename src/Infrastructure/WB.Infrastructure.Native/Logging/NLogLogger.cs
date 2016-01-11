﻿using System;
using NLog;
using ILogger = WB.Core.GenericSubdomains.Portable.Services.ILogger;

namespace WB.Infrastructure.Native.Logging
{
    internal class NLogLogger : ILogger
    {
        private readonly Logger logger;

        public NLogLogger()
        {
            this.logger = LogManager.GetLogger("");
        }

        public NLogLogger(Type type)
        {
            this.logger = LogManager.GetLogger(type.FullName);
        }

        public void Debug(string message, Exception exception = null)
        {
            this.logger.Debug(exception, message);
        }

        public void Info(string message, Exception exception = null)
        {
            this.logger.Info(exception, message);
        }

        public void Warn(string message, Exception exception = null)
        {
            this.logger.Warn(exception, message);
        }

        public void Error(string message, Exception exception = null)
        {
            this.logger.Error(exception, message);
        }

        public void Fatal(string message, Exception exception = null)
        {
            this.logger.Fatal(exception, message);
        }
    }
}
