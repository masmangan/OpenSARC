using System;
using System.Collections.Generic;
using System.Text;
using BusinessData.Entities;
using BusinessData.DataAccess;
//Log
//using Microsoft.Practices.EnterpriseLibrary.Logging;
using System.Net;
using System.Diagnostics;
using System.Web.Security;

namespace BusinessData.BusinessLogic
{
    public class ControladorDistribuirAulasBO
    {
        private AulaBO aulasBO;
        private AlocacaoBO alocacaoBO ;
        private ConfigBO configBO;

        public ControladorDistribuirAulasBO()
        {
            aulasBO = new AulaBO();
            alocacaoBO = new AlocacaoBO();
            configBO = new ConfigBO();
        }

        public void ResolveCagada(Calendario cal)
        {
            //aulasBO.CriarAulasCompletar(cal);
            alocacaoBO.preencheCalendario(cal, true);
            //aulasBO.ResolveCagada(cal);
        }

        public void AbreSolicitacaoRecursos(Calendario cal, bool completar=false)
        {
            try
            {
                if (completar)
                    aulasBO.CriarAulasCompletar(cal);
                else
                    aulasBO.CriarAulas(cal);
                alocacaoBO.preencheCalendario(cal, completar);
                if(!completar)
                    configBO.setAulasDistribuidas(cal, true);

                //instancia o usuario logado
                MembershipUser user = Membership.GetUser();
                //instancia o log
                //LogEntry log = new LogEntry();
                //monta log
                //log.Message = "Calend�rio: " + cal.Ano + "/" + cal.Semestre +"; Administrador: " + user.UserName;
                //log.TimeStamp = DateTime.Now;
                //log.Severity = TraceEventType.Information;
                //log.Title = "Abrir solicita��o de Recursos";
                //log.MachineName = Dns.GetHostName();
                //guarda log no banco
                //Logger.Write(log);
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new DataAccessException("N�o h� categorias de atividades cadastradas no sistema.", ex);
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Ocorreu um erro inesperado.", ex);
            }
        }


    }
}
