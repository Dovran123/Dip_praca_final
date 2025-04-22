using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;
using Domain.Entities;
using Application.Common.Models;

[ServiceContract]
public interface IPreskripciaService
{

    // PrihlaseniePrimatel
    [OperationContract]
    Task<string> SpravPrimatelov(string supertokenPrimatela, List<Guid> idPouzivatelov);

    // 🏥 Pacienti
    [OperationContract]
    Task<Poistenie> OverPoistencaAsync(string? rC, string? iCP, DateTime? datum);
       
    // 💊 Lieky
    [OperationContract]
    Task<List<Liek>> GetLiekyAsync();
    [OperationContract]
    Task<Liek?> GetLiekByKodAsync(string kod);
        // Diagnozy
    [OperationContract]
    Task<List<Diagnoza>> GetDiagnozyAsync();

        // 📦 Kategórie liekov
    
    [OperationContract]
    Task<List<KategoriaLiekov>> GetKategoriaLiekovAsync();
    // Potraviny

    [OperationContract]
    Task<List<Potravina>> GetPotravinyAsync();

    [OperationContract]
    Task<List<Vyrobky>> GetPotravinyByKategoriaAsync(string kodKategorie);

    [OperationContract]
    Task<List<KategoriaDTO>> GetKategoriePotravinAsync();

    // Pacienti
    [OperationContract]
    Task<List<PacientDto>> GetPacientiAsync();
    [OperationContract]
    Task<Pouzivatel> VytvorPouzivatela(string meno, string priezvisko, string email, string heslo, string RC);
    [OperationContract]
    Task<string> DajTokenPrimatela(string rc, string heslo);
    // 🏥 Lekári
    [OperationContract]
    Task<List<Lekar>> GetLekariAsync();
  
    // 📦 Doplnky
    [OperationContract]
    Task<List<Doplnok>> GetDoplnkyAsync();

    [OperationContract]
    Task<List<KategoriaDTO>> GetKategoriaDoplnkovAsync();

    [OperationContract]
    Task<List<Vyrobky>> GetDoplnkyByKategoriaAsync(string kodKategorie);
    // 📜 Predpisy

    [OperationContract]
    Task<UlozPreskripcnyZaznamResponse> UlozPreskripcnyZaznamAsync(string tokenPrimatela, ApiPreskripcnyZaznam preskripcny);
    [OperationContract]
    Task<InterakciaResponse> OverInterakciePacientaAsync(
            string? rC,
            string? iCP,
            string[] kodyLiekov,
            string obdobieOd,
            string obdobieDo);
    [OperationContract]
    Task<StornujPreskripcnyZaznamResponse> StornujPreskripcnyZaznamAsync(Guid idPreskripcnehoZaznamu, Guid idLekara);
    // 📜 Preskripcie
    [OperationContract]
    Task<string> GetPrimatelToken(Guid idUser, string password);
    [OperationContract]
    Task<VyhladajPreskripcnyZaznamResponse> VyhladajPreskripcnyZaznamAsync(string tokenPrijimatelZS, string? rC, string? iCP);
    [OperationContract]
    Task<ApiPreskripcnyZaznam> DajPreskripcnyZaznam(Guid idPreskripcnehoZaznamu);
    [OperationContract]
    Task<BlokujPreskripcnyZaznamResponse> BlokujPreskripcnyZaznamAsync(APIUdajePreBlokovanie udajePreBlokovanie);
    [OperationContract]
    // LimitPredpisov
    Task<SkontrolujPredpisPomockyResponse> SkontrolujPredpisPomocky(
     string rodneCislo, string kodPomocky, decimal mnozstvo, DateTime? datumPredpisu,
     string kodOdbLekara, string? diagnoza);
}

