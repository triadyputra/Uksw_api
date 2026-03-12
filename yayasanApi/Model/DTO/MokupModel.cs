namespace yayasanApi.Model.DTO
{
    public class ComboModel
    {
        public string value { get; set; }

        public string label { get; set; }
    }


    #region Menu_App

    public class MenuViewModel
    {
        public string IdMenu { get; set; }
        public int NoUrut { get; set; }
        public string NamaMenu { get; set; }
        public ICollection<ControllerViewModel> ControllerViewModel { get; set; }
    }

    public class ControllerViewModel
    {
        public string IdController { get; set; }
        public int NoUrut { get; set; }
        public string Controller { get; set; }
        public string IdMenu { set; get; }
        //public string Modul { set; get; }
        public ICollection<ActionViewModel> ActionViewModel { get; set; }
    }

    public class ActionViewModel
    {
        public string IdAction { get; set; }
        public int NoUrut { get; set; }
        public string NamaAction { get; set; }
        public string IdController { set; get; }
        public string modul { set; get; }

    }

    public class AccesModel
    {
        public string IdController { get; set; }
        public string IdAction { get; set; }
    }

    public class FilterMenu
    {
        public string IdAction { get; set; }
        public string NameAction { get; set; }
    }

    public class FilterMenuWeb
    {
        public string subject { get; set; }
        public string action { get; set; }

    }
    #endregion

}
