using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Samostaytelnay
{
    internal class HtmlTemp
    {
        public string RenderIndexPage(List<Movie> movies, string currentFilter)
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><title>Каталог фильмов</title></head><body>");
            sb.Append("<h1>Каталог фильмов</h1>");

            sb.Append("<form method='GET' action='/movies'>");
            sb.Append("Жанр: <select name='genre'><option value=''>Все</option>");
            foreach (var g in new[] { "drama", "comedy", "action" })
            {
                string selected = g == currentFilter ? "selected" : "";
                sb.Append($"<option value='{g}' {selected}>{g}</option>");
            }
            sb.Append("</select> <input type='submit' value='Фильтр'></form><br>");

            sb.Append("<table border='1'><tr><th>ID</th><th>Название</th><th>Жанр</th><th>Статус</th><th>Действия</th></tr>");
            foreach (var m in movies)
            {
                sb.Append($"<tr><td>{m.Id}</td>");
                sb.Append($"<td>{WebUtility.HtmlEncode(m.Title)}</td>");
                sb.Append($"<td>{m.Genre}</td>");
                sb.Append($"<td>{m.Status}</td>");
                sb.Append($"<td><a href='/movies/details?id={m.Id}'>Редактировать</a></td></tr>");
            }
            sb.Append("</table>");

            sb.Append("<h2>Добавить фильм</h2>");
            sb.Append("<form method='POST' action='/movies/create'>");
            sb.Append("Название: <input type='text' name='title' required><br><br>");
            sb.Append("Жанр: <select name='genre'><option value='drama'>drama</option><option value='comedy'>comedy</option><option value='action'>action</option></select><br><br>");
            sb.Append("<input type='submit' value='Создать'>");
            sb.Append("</form></body></html>");

            return sb.ToString();
        }
        public string RenderDetailsPage(Movie movie)
        {
            return $@"<html><head><title>Фильм {movie.Id}</title>
            <script>
                async function sendPut(event) {{
                    event.preventDefault();
                    const formData = new URLSearchParams(new FormData(event.target));
                    const res = await fetch('/movies/update?id={movie.Id}', {{
                        method: 'PUT',
                        headers: {{ 'Content-Type': 'application/x-www-form-urlencoded' }},
                        body: formData.toString()
                    }});
                    if (res.status === 204) {{
                        alert('Обновлено!'); window.location.href = '/movies';
                    }} else {{
                        document.body.innerHTML = await res.text();
                    }}
                }}
            </script></head><body>
            <h1>Редактирование фильма №{movie.Genre}</h1>
            <form onsubmit='sendPut(event)'>
                Название: <input type='text' name='title' value='{WebUtility.HtmlEncode(movie.Title)}' required><br><br>
                Жанр: <select name='genre'>
                    <option value='drama' {(movie.Genre == "drama" ? "selected" : "")}>drama</option>
                    <option value='comedy' {(movie.Genre == "comedy" ? "selected" : "")}>comedy</option>
                    <option value='action' {(movie.Genre == "action" ? "selected" : "")}>action</option>
                </select><br><br>
                Статус: <select name='status'>
                    <option value='planned' {(movie.Status == "planned" ? "selected" : "")}>planned</option>
                    <option value='watched' {(movie.Status == "watched" ? "selected" : "")}>watched</option>
                </select><br><br>
                <input type='submit' value='Сохранить'>
            </form>
            <p><a href='/movies'>Назад к списку</a></p>
            </body></html>";
        }
    }
}
