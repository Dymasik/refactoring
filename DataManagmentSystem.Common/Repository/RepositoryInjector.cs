
using DataManagementSystem.ReportService.Configuration;
using DataManagmentSystem.Auth.Injector;
using DataManagmentSystem.Common.Email;
using DataManagmentSystem.Common.Locale;
using DataManagmentSystem.Common.Request;
using DataManagmentSystem.Common.SelectQuery;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace DataManagmentSystem.Common.Repository
{
    public class RepositoryInjector<TContext>
		where TContext : DbContext
    {
		public TContext Context { get; set; }
		public IObjectLocalizer Localizer { get; set; }
		public ILocalizationEntityResolver LocalizeResolver { get; set; }
		public IUserDataAccessor UserDataAccessor { get; set; }
		public IEmailService<MimeMessage> EmailService { get; set; }
		public ReportService ReportService { get; set; }
		public IColumnToSelectConverter SelectColumnConverter { get; set; }
		public IFilterToExpressionConverter FilterConverter { get; set; }
		public IMediator Mediator { get; set; }
	}
}
