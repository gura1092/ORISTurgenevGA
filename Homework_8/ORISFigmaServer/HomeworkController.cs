using MiniHttpServer.Framework;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ORISFigmaServer
{
    [HttpController]
    internal class HomeworkController
    {
        public void Send(string to)
        {
            Console.WriteLine($"\n[Почта]: Запрос на отправку домашнего задания для {to}");

            string groupNumber = "11-541";
            string fullName = "Тургенев Григорий Александрович";

            string smtpHost = "smtp.yandex.ru";
            int smtpPort = 587;
            string fromEmail = "gaturgenev@kpfu.ru";
            string fromPassword = "xvrdrluuaaczvvbj";

            string projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

            string zipPath = Path.Combine(Path.GetTempPath(), $"Project_{groupNumber}_{Guid.NewGuid().ToString().Substring(0, 8)}.zip");

            try
            {
                if (File.Exists(zipPath)) File.Delete(zipPath);

                ZipFile.CreateFromDirectory(projectPath, zipPath);
                Console.WriteLine($"[Почта]: Архив успешно создан по пути: {zipPath}");

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(fromEmail, fullName);
                    mail.To.Add(to);
                    mail.Subject = $"Домашнее задание - Группа {groupNumber} - {fullName}";
                    mail.Body = $"Здравствуйте!\n\nВысылаю выполненное домашнее задание.\n\nСтудент: {fullName}\nГруппа: {groupNumber}";

                    using (Attachment attachment = new Attachment(zipPath))
                    {
                        mail.Attachments.Add(attachment);

                        using (SmtpClient smtp = new SmtpClient(smtpHost, smtpPort))
                        {
                            smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
                            smtp.EnableSsl = true;
                            smtp.Send(mail);
                        }
                    }
                }

                Console.WriteLine("[Почта]: Письмо успешно отправлено!");
            }
            catch (Exception ex)
            {
                Exception realException = ex is System.Reflection.TargetInvocationException ? ex.InnerException : ex;
                Console.WriteLine("КРИТИЧЕСКАЯ ОШИБКА В КОНТРОЛЛЕРЕ: " + realException.Message);
                Console.WriteLine(realException.StackTrace);

                throw realException;
            }
            finally
            {
                if (File.Exists(zipPath))
                {
                    try { File.Delete(zipPath); } catch { }
                }
            }
        }
    }
}