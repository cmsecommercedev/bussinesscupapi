document.addEventListener('DOMContentLoaded', function() {
    // Sekme değiştiğinde ligi yükle
    const leagueTabs = document.querySelectorAll('[data-league-tab]');
    leagueTabs.forEach(tab => {
        tab.addEventListener('click', async function() {
            const leagueId = this.dataset.leagueId;
            
            try {
                const response = await fetch(`/Admin/GetLeagueData/${leagueId}`);
                if (!response.ok) throw new Error('Lig verileri yüklenemedi');
                
                const html = await response.text();
                document.getElementById('leagueContent').innerHTML = html;
                
                // Aktif sekmeyi güncelle
                leagueTabs.forEach(t => t.classList.remove('active'));
                this.classList.add('active');
            } catch (error) {
                console.error('Lig yüklenirken hata:', error);
            }
        });
    });
}); 