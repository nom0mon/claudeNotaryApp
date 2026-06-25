using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

public class FirestoreRepository<T> : IFirestoreRepository<T> where T : class
{
    private readonly CollectionReference _collection;

    public FirestoreRepository(FirestoreDb db, string collectionPath)
    {
        _collection = db.Collection(collectionPath);
    }

    public async Task<string> AddAsync(T entity)
    {
        var docRef = await _collection.AddAsync(entity);
        return docRef.Id;
    }

    public async Task<T> GetAsync(string id)
    {
        var snap = await _collection.Document(id).GetSnapshotAsync();
        return snap.Exists ? snap.ConvertTo<T>() : null;
    }

    public Task UpdateAsync(string id, Dictionary<string, object> fields)
    {
        return _collection.Document(id).UpdateAsync(fields);
    }

    public Task DeleteAsync(string id)
    {
        return _collection.Document(id).DeleteAsync();
    }

    // This powers the real-time sync across multi-devices (Section 7.5)
    public IObservable<List<T>> ListenAll(Query query = null)
    {
        var q = query ?? _collection;
        return Observable.Create<List<T>>(observer =>
        {
            var listener = q.Listen(snapshot =>
            {
                var items = snapshot.Documents
                    .Where(d => d.Exists)
                    .Select(d => d.ConvertTo<T>())
                    .ToList();
                observer.OnNext(items);
            });
            return () => listener.StopAsync();
        });
    }
}