﻿using System;
using System.Collections.Generic;
using System.Linq;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;

namespace Inversion.Data.Couchbase
{
    public class CouchbaseStore<T> : StoreBase, IStoreHealth
    {
        protected readonly Cluster Cluster;
        protected IBucket Bucket;
        protected readonly string BucketName;
        private bool _disposed;

        public Document<T> this[string key]
        {
            get
            {
                IDocumentResult<T> result = this.Bucket.GetDocument<T>(key);
                return result.Success ? result.Document : null;
            }
            set
            {
                this.Bucket.Upsert(value);
            }
        }

        public CouchbaseStore(IEnumerable<string> uris, string bucketName)
        {
            this.Cluster = new Cluster(
                new ClientConfiguration
                {
                    Servers = new List<Uri>(uris.Select(u => new Uri(u)))
                }
            );
            this.BucketName = bucketName;
        }

        public override void Start()
        {
            base.Start();
            this.Bucket = this.Cluster.OpenBucket(this.BucketName);
        }

        public sealed override void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            if (this.HasStarted)
            {
                if (this.Bucket != null)
                {
                    this.Bucket.Dispose();
                }
            }

            if (this.Cluster != null)
            {
                this.Cluster.Dispose();
            }
        }

        public virtual bool GetHealth(out string result)
        {
            this.AssertIsStarted();

            result = String.Empty;

            if (this.Cluster.IsOpen(this.BucketName))
            {
                return true;
            }

            result = String.Format("Cluster reports bucket '{0}' not being observed.", this.BucketName);
            return false;
        }
    }
}