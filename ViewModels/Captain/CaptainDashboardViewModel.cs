using System;
using System.Collections.Generic;

namespace BussinessCupApi.ViewModels.Captain
{
    public class CaptainDashboardViewModel
    {
        public List<CaptainMatchViewModel> Matches { get; set; } = new List<CaptainMatchViewModel>();
        public int CaptainTeamId { get; set; }
    }
} 