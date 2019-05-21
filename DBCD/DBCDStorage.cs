using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;

using DB2FileReaderLib.NET;

namespace DBCD
{
    public class DBCDRow : DynamicObject
    {
        public int ID;

        private dynamic raw;

        internal DBCDRow(dynamic raw)
        {
            this.raw = raw;
            this.ID = raw.ID;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return this.raw.TryGetMember(binder, out result);
        }
    }

    public class DynamicKeyValuePair<T>
    {
        public T Key;
        public dynamic Value;

        internal DynamicKeyValuePair(T key, dynamic value)
        {
            this.Key = key;
            this.Value = value;
        }
    }

    public interface IDBCDStorage : IEnumerable<DynamicKeyValuePair<int>>, ILookup<int, DBCDRow>
    {
        string[] availableColumns { get; }

        IEnumerable<dynamic> Values { get; }

        IEnumerable<int> Keys { get; }
    }

    public class DBCDStorage<T> : Storage<T>, IDBCDStorage where T : class, new()
    {
        private string[] availableColumns;
        private string tableName;
        string[] IDBCDStorage.availableColumns => this.availableColumns;

        public DBCDStorage(Stream stream, DBCDInfo info) : base(stream)
        {
            this.availableColumns = info.availableColumns;
            this.tableName = info.tableName;
        }


        private IEnumerable<DBCDRow> DynamicValues => this.Values.Select(row => new DBCDRow(row));
        IEnumerable<dynamic> IDBCDStorage.Values => this.DynamicValues;
        IEnumerable<int> IDBCDStorage.Keys => this.Keys;

        IEnumerable<DBCDRow> ILookup<int, DBCDRow>.this[int key] => this.DynamicValues.Where(row => row.ID == key);

        IEnumerator<DynamicKeyValuePair<int>> IEnumerable<DynamicKeyValuePair<int>>.GetEnumerator()
        {
            return this.DynamicValues.Select(row => new DynamicKeyValuePair<int>(row.ID, row)).GetEnumerator();
        }

        public override string ToString()
        {
            return $"{this.tableName}";
        }

        public bool Contains(int key)
        {
            return this.Keys.Contains(key);
        }

        IEnumerator<IGrouping<int, DBCDRow>> IEnumerable<IGrouping<int, DBCDRow>>.GetEnumerator()
        {
            return this.DynamicValues.GroupBy(row => row.ID).GetEnumerator();
        }
    }
}