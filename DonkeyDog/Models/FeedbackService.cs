using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;


namespace DonkeyDog.Models
{
    public class FeedbackService
    {
        private readonly IMongoCollection<Feedback> _feedbackCollection;
        private readonly IConfiguration _configuration;

        public FeedbackService(IMongoDatabase database, IConfiguration configuration)
        {
            _feedbackCollection = database.GetCollection<Feedback>("Feedback");
            _configuration = configuration;
        }

        public async Task<List<Feedback>> GetAllAsync()
        {
            return await _feedbackCollection.Find(_ => true).ToListAsync();
        }

        public async Task CreateAsync(Feedback feedback)
        {
            feedback.DateCreated = DateTime.UtcNow;
            // Prova a inserire il documento, rigenera l'Id in caso di duplicato
            while (true)
            {
                try
                {
                    feedback.Id = Guid.NewGuid(); // Assicurati che l'ID sia nuovo
                    await _feedbackCollection.InsertOneAsync(feedback);
                    break; // Esci dal ciclo se l'inserimento ha successo
                }
                catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    // In caso di errore di chiave duplicata, rigenera l'Id e riprova
                    feedback.Id = Guid.NewGuid();
                }
            }

            await SendEmailAsync(feedback);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _feedbackCollection.DeleteOneAsync(fb => fb.Id == id);
        }

        private async Task SendEmailAsync(Feedback feedback)
        {
            var smtpSection = _configuration.GetSection("Smtp");

            var smtpClient = new SmtpClient(smtpSection["Host"])
            {
                Port = int.Parse(smtpSection["Port"]),
                Credentials = new NetworkCredential(smtpSection["Username"], smtpSection["Password"]),
                EnableSsl = bool.Parse(smtpSection["EnableSsl"]),
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSection["From"]),
                Subject = "Nuovo Messaggio Ricevuto",
                Body = $"Hai ricevuto un nuovo messaggio da {feedback.Name} ({feedback.Email}):\n\n{feedback.Message}",
                IsBodyHtml = false,
            };

            mailMessage.To.Add("charyassine2004@gmail.com"); 

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Gestisci eventuali errori
                Console.WriteLine($"Errore nell'invio dell'email: {ex.Message}");
            }
        }
    }
}
