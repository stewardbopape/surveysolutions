﻿namespace WB.Core.Infrastructure.Raven
{
    public class RavenConnectionSettings
    {
        public RavenConnectionSettings(string storagePath, bool isEmbedded = false,
            string username = null, string password = null, string eventsDatabase = "Events", string viewsDatabase = "Views", string nonCqrsDatabase = "Storage")
        {
            this.IsEmbedded = isEmbedded;
            this.Username = username;
            this.Password = password;
            this.StoragePath = storagePath;
            this.EventsDatabase = eventsDatabase;
            this.ViewsDatabase = viewsDatabase;
            this.NonCqrsDatabase = nonCqrsDatabase;
        }

        public bool IsEmbedded { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string StoragePath { get; private set; }
        public string EventsDatabase { get; private set; }
        public string ViewsDatabase { get; private set; }
        public string NonCqrsDatabase { get; private set; }
    }
}