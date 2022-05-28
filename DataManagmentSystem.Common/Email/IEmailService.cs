namespace DataManagmentSystem.Common.Email {
    using DataManagementSystem.EmailService.EmailBodyRenderer;
    using DataManagementSystem.EmailService.EmailBuilder;
    using DataManagementSystem.EmailService.EmailSender;

    public interface IEmailService<TEmail> where TEmail : class {
        public IEmailSender<TEmail> Sender { get; set; }
        public IEmailBuilder<TEmail> Builder { get; set; }
        public IEmailBodyFromViewRenderer Renderer { get; set; }
    }
}
