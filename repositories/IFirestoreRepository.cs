using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

public interface IFirestoreRepository<T> where T : class
{
    Task<string> AddAsync(T entity);
    Task<T> GetAsync(string id);
    Task UpdateAsync(string id, Dictionary<string, object> fields);
    Task DeleteAsync(string id);
    IObservable<List<T>> ListenAll(Query query = null);
}