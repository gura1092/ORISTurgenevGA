using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samostaytelnay
{
    internal class MovieLogic
    {
        public bool Validate(string title, string genre, string status, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(title) || title.Length < 2 || title.Length > 100)
            {
                error = "Название фильма должно содержать от 2 до 100 символов.";
                return false;
            }

            string[] validGenres = { "drama", "comedy", "action" };
            if (string.IsNullOrEmpty(genre) || !validGenres.Contains(genre.ToLower()))
            {
                error = "Допустимые жанры: drama, comedy, action.";
                return false;
            }

            string[] validStatuses = { "planned", "watched" };
            if (string.IsNullOrEmpty(status) || !validStatuses.Contains(status.ToLower()))
            {
                error = "Допустимые статусы: planned, watched.";
                return false;
            }
            return true;
        }

        public List<Movie> FilterAndSort(List<Movie> source, string genreFilter)
        {
            var query = source.AsQueryable();

            if (!string.IsNullOrWhiteSpace(genreFilter))
            {
                query = query.Where(m => m.Genre.Equals(genreFilter, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderBy(m => m.Status == "planned" ? 0 : 1)
                        .ThenBy(m => m.Id)
                        .ToList();
        }
    }
}
