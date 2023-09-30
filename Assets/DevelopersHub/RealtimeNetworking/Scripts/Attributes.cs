namespace DevelopersHub.RealtimeNetworking.Client
{
    using System;
    using UnityEngine;

    public class ReadOnlyAttribute : PropertyAttribute { }

    [AttributeUsage(AttributeTargets.Field)] public class SyncVariable : PropertyAttribute
    {

        public enum WhoCanChange
        {
            Owner = 1, Host = 3
        }

        public WhoCanChange whoCanChange = WhoCanChange.Owner;

        public SyncVariable(WhoCanChange whoCanChange) 
        {
            this.whoCanChange = whoCanChange;
        }

        public SyncVariable()
        {
            this.whoCanChange |= WhoCanChange.Owner;
        }

    }

}