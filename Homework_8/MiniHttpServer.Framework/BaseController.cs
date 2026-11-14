namespace MiniHttpServer.Framework
{
    public abstract class BaseController
    {
        // Вспомогательный метод для возврата HTML-страницы из контроллера
        protected IHttpResult Page(string templatePath, object dataModel = null)
        {
            return new PageResult(templatePath, dataModel);
        }
    }
}
