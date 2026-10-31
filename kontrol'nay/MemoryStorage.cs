using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samostaytelnay
{
    internal class MemoryStorage
    {
        public class InMemoryStorage
        {
            private readonly List<Movie> _movies = new List<Movie>();
            private int _nextId = 1;
            private readonly object _lock = new object();

            public Movie Insert(string title, string genre)
            {
                lock (_lock)
                {
                    var movie = new Movie
                    {
                        Id = _nextId++,
                        Title = title,
                        Genre = genre.ToLower(),
                        Status = "planned"
                    };
                    _movies.Add(movie);
                    return movie;
                }
            }

            public bool Modify(int id, string title, string genre, string status)
            {
                lock (_lock)
                {
                    var movie = _movies.FirstOrDefault(m => m.Id == id);
                    if (movie == null) return false;

                    movie.Title = title;
                    movie.Genre = genre.ToLower();
                    movie.Status = status.ToLower();
                    return true;
                }
            }

            public Movie FindById(int id)
            {
                lock (_lock) return _movies.FirstOrDefault(m => m.Id == id);
            }

            public List<Movie> FetchAll()
            {
                lock (_lock) return _movies.ToList();
            }
        }
    }
}
