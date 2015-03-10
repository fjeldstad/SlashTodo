using System;
using System.Threading.Tasks;
using SlashTodo.Core.Domain;

namespace SlashTodo.Core
{
    public interface IRepository<T> where T : Aggregate, new()
    {
        Task<T> GetById(string id);
        Task Save(T aggregate);
    }
}