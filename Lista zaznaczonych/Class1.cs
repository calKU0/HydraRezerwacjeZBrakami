using System;
using Hydra;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Data.SqlClient;

[assembly: CallbackAssemblyDescription("Ostrzeżenie rezerwacje z brakiem",
"Ostrzeżenie o braku stanu przy rezerwacji",
"Krzysztof Kurowski",
"1.0",
"8.0",
"22-07-2024")]

namespace Dokument_Zamowienia
{
    [SubscribeProcedure((Procedures)Procedures.ZaNZamEdycjaSpr, "callback na dokumencie")]
    public class callbackOnOrder : Callback
    {
        private List<string> TwrGids;
        private List<string> TwrCodes;
        private List<decimal> Quantities;

        private ClaWindow Parent;
        private ClaWindow Zapisz;
        private ClaWindow Bufor;

        private int StartState;
        private int EndState;

        private readonly string ConnectionString = $"user id=xxxx;password=xxxx;Data Source=xxxx;Trusted_Connection=no;database={Runtime.ActiveRuntime.Repository.Connection.Database};connection timeout=5;";

        public override void Init()
        {
            AddSubscription(true, 0, Events.OpenWindow, new TakeEventDelegate(OnOpenWindow)); // Otwarcie okna
            // AddSubscription(false, 0, Events.ResizeWindow, new TakeEventDelegate(ChangeWindow)); // zmiana szerokosci/wysokosci okna
        }

        public bool OnOpenWindow(Procedures ProcId, int ControlId, Events Event)
        {
            Parent = GetWindow();

            StartState = ZamNag.ZaN_Stan;

            Zapisz = Parent.AllChildren["?Cli_Zapisz"];

            Zapisz.OnBeforeAccepted += new TakeEventDelegate(ShowWarning);

            return true; 
        }


        public bool ShowWarning(Procedures ProcId, int ControlId, Events Event)
        {
            try
            {
                TwrGids = new List<string>();
                TwrCodes = new List<string>();
                Quantities = new List<decimal>();

                EndState = ZamNag.ZaN_Stan;
                if (StartState != EndState && StartState <= 2 && EndState > 2)
                {
                    CheckProductsWithoutQuantity();
                    if (TwrGids.Count > 0)
                    {
                        string message = $"Rezerwacja bez zasobu{Environment.NewLine}";
                        for (int i = 0; i < TwrGids.Count; i++)
                        {
                            message += $"{Environment.NewLine}Towar: {TwrCodes[i]}. Brak zasobów do zarezerwowania. Ilość: {Quantities[i]}";
                        }
                        Runtime.WindowController.UnlockThread();
                        MessageBox.Show(message);
                        Runtime.WindowController.LockThread();
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return true;
            }
            return true;
        }


        public bool ChangeWindow(Procedures ProcId, int ControlId, Events Event)
        {
            return true;
        }


        public override void Cleanup()
        {

        }

        public void CheckProductsWithoutQuantity()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    string query = @"select Rez_TwrNumer as [TwrNumber],Twr_Kod as [TwrCode],Rez_Ilosc as [Quantity]
from cdn.ZamNag 
join cdn.Rezerwacje with(nolock) on ZaN_GIDNumer = Rez_ZrdNumer
join cdn.TwrKarty with(nolock) on Rez_TwrNumer = twr_gidnumer
where ZaN_GIDTyp=@ZanGidTyp AND ZaN_GIDNumer=@ZanGidNumer and Rez_DstNumer = 0";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ZanGidNumer", ZamNag.ZaN_GIDNumer);
                    command.Parameters.AddWithValue("@ZanGidTyp", ZamNag.ZaN_GIDTyp);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TwrGids.Add(reader["TwrNumber"].ToString());
                            TwrCodes.Add(reader["TwrCode"].ToString());
                            Quantities.Add(decimal.Parse(reader["Quantity"].ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

    }

}
