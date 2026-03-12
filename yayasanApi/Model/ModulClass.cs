using yayasanApi.Model.DTO;

namespace yayasanApi.Model
{
    public class ModulClass
    {
        private List<MenuViewModel> _mvcMenu;
        private List<ControllerViewModel> _mvcConntroller;

        public List<MenuViewModel> MenuApp()
        {
            return new List<MenuViewModel>{
                new MenuViewModel{
                    IdMenu = "Beranda",
                    NoUrut = 1,
                    NamaMenu = "Beranda",
                },

                new MenuViewModel{
                    IdMenu = "Konfigurasi",
                    NoUrut = 2,
                    NamaMenu = "Konfigurasi",
                },

                new MenuViewModel{
                    IdMenu = "MasterData",
                    NoUrut = 3,
                    NamaMenu = "Master Data",
                },

                new MenuViewModel{
                    IdMenu = "Transaksi",
                    NoUrut = 4,
                    NamaMenu = "Transaksi",
                },
            };
        }

        public List<ControllerViewModel> Controller()
        {
            return new List<ControllerViewModel>{
                #region Beranda
                new ControllerViewModel{
                    IdController = "Beranda",
                    NoUrut = 1,
                    Controller = "Beranda",
                    IdMenu = "Beranda",
                },
                #endregion
              
                #region Konfigurasi
                new ControllerViewModel{
                    IdController = "Role",
                    NoUrut = 1,
                    Controller = "Role",
                    IdMenu = "Konfigurasi",
                },
                new ControllerViewModel{
                    IdController = "Akun",
                    NoUrut = 2,
                    Controller = "Akun",
                    IdMenu = "Konfigurasi",
                },
                #endregion

                #region Master-Data
                new ControllerViewModel{
                    IdController = "MasterCoa",
                    NoUrut = 1,
                    Controller = "Master Coa",
                    IdMenu = "MasterData",
                },
                new ControllerViewModel{
                    IdController = "MasterUnit",
                    NoUrut = 2,
                    Controller = "Master Unit",
                    IdMenu = "MasterData",
                },
                new ControllerViewModel{
                    IdController = "MappingCoa",
                    NoUrut = 3,
                    Controller = "Mapping Coa",
                    IdMenu = "MasterData",
                },
                #endregion

                #region Transaksi
                new ControllerViewModel{
                    IdController = "UploadLaporan",
                    NoUrut = 1,
                    Controller = "Laporan Keuangan",
                    IdMenu = "Transaksi",
                },
                new ControllerViewModel{
                    IdController = "Eleminasi",
                    NoUrut = 2,
                    Controller = "Eleminasi",
                    IdMenu = "Transaksi",
                },
                new ControllerViewModel{
                    IdController = "Laporan",
                    NoUrut = 3,
                    Controller = "Laporan",
                    IdMenu = "Transaksi",
                },
                #endregion  
            };
        }

        public List<ActionViewModel> Action()
        {
            return new List<ActionViewModel>{
                #region Beranda
                new ActionViewModel{
                    IdAction = "Read",
                    NoUrut = 1,
                    NamaAction = "Lihat",
                    IdController = "Beranda",
                },
                #endregion

                #region Konfigurasi
                /* Master Role */
                new ActionViewModel{
                    IdAction = "GetListRole",
                    NoUrut = 1,
                    NamaAction = "Lihat",
                    IdController = "Role",
                },
                new ActionViewModel{
                    IdAction = "PostRole",
                    NoUrut = 2,
                    NamaAction = "Tambah",
                    IdController = "Role",
                },
                new ActionViewModel{
                    IdAction = "PutRole",
                    NoUrut = 3,
                    NamaAction = "Edit",
                    IdController = "Role",
                },
                new ActionViewModel{
                    IdAction = "DeleteRole",
                    NoUrut = 4,
                    NamaAction = "Hapus",
                    IdController = "Role",
                },

                /* Master Akun */
                new ActionViewModel{
                    IdAction = "GetListAkun",
                    NoUrut = 1,
                    NamaAction = "Lihat",
                    IdController = "Akun",
                },
                new ActionViewModel{
                    IdAction = "PostAkun",
                    NoUrut = 2,
                    NamaAction = "Tambah",
                    IdController = "Akun",
                },
                new ActionViewModel{
                    IdAction = "PutAkun",
                    NoUrut = 3,
                    NamaAction = "Edit",
                    IdController = "Akun",
                },
                new ActionViewModel{
                    IdAction = "DeleteAkun",
                    NoUrut = 4,
                    NamaAction = "Hapus",
                    IdController = "Akun",
                },
               
                
                #endregion

                #region Master-Data
                /* Master Coa */
                new ActionViewModel{
                    IdAction = "GetListCoa",
                    NoUrut = 1,
                    NamaAction = "Lihat",
                    IdController = "MasterCoa",
                },
                new ActionViewModel{
                    IdAction = "PostCoa",
                    NoUrut = 2,
                    NamaAction = "Tambah",
                    IdController = "MasterCoa",
                },
                new ActionViewModel{
                    IdAction = "PutCoa",
                    NoUrut = 3,
                    NamaAction = "Edit",
                    IdController = "MasterCoa",
                },
                new ActionViewModel{
                    IdAction = "DeleteCoa",
                    NoUrut = 4,
                    NamaAction = "Hapus",
                    IdController = "MasterCoa",
                },

                /* Master Unit */
                new ActionViewModel{
                    IdAction = "GetListUnit",
                    NoUrut = 1,
                    NamaAction = "Lihat",
                    IdController = "MasterUnit",
                },
                new ActionViewModel{
                    IdAction = "PostUnit",
                    NoUrut = 2,
                    NamaAction = "Tambah",
                    IdController = "MasterUnit",
                },
                new ActionViewModel{
                    IdAction = "PutUnit",
                    NoUrut = 3,
                    NamaAction = "Edit",
                    IdController = "MasterUnit",
                },
                new ActionViewModel{
                    IdAction = "DeleteUnit",
                    NoUrut = 4,
                    NamaAction = "Hapus",
                    IdController = "MasterUnit",
                },
               
                /* Mapping COa */
                new ActionViewModel{
                    IdAction = "GetListMapping",
                    NoUrut = 1,
                    NamaAction = "Lihat",
                    IdController = "MappingCoa",
                },
                new ActionViewModel{
                    IdAction = "PostMapping",
                    NoUrut = 2,
                    NamaAction = "Tambah",
                    IdController = "MappingCoa",
                },
                new ActionViewModel{
                    IdAction = "PutMapping",
                    NoUrut = 3,
                    NamaAction = "Edit",
                    IdController = "MappingCoa",
                },
                new ActionViewModel{
                    IdAction = "DeleteMapping",
                    NoUrut = 4,
                    NamaAction = "Hapus",
                    IdController = "MappingCoa",
                },
                #endregion
          
                #region Transaksi
                /* Upload Laporan */
                new ActionViewModel{
                    IdAction = "GetListLaporan",
                    NoUrut = 1,
                    NamaAction = "Lihat",
                    IdController = "UploadLaporan",
                },
                new ActionViewModel{
                    IdAction = "PostLaporan",
                    NoUrut = 2,
                    NamaAction = "Tambah",
                    IdController = "UploadLaporan",
                },
                new ActionViewModel{
                    IdAction = "PutLaporan",
                    NoUrut = 3,
                    NamaAction = "Edit",
                    IdController = "UploadLaporan",
                },
                new ActionViewModel{
                    IdAction = "DeleteLaporan",
                    NoUrut = 4,
                    NamaAction = "Hapus",
                    IdController = "UploadLaporan",
                },

                /* Eleminasi */
                new ActionViewModel{
                    IdAction = "GetListEleminasi",
                    NoUrut = 1,
                    NamaAction = "Lihat",
                    IdController = "Eleminasi",
                },
                new ActionViewModel{
                    IdAction = "PostEleminasi",
                    NoUrut = 2,
                    NamaAction = "Tambah",
                    IdController = "Eleminasi",
                },
                new ActionViewModel{
                    IdAction = "PutEleminasi",
                    NoUrut = 3,
                    NamaAction = "Edit",
                    IdController = "Eleminasi",
                },
                new ActionViewModel{
                    IdAction = "DeleteEleminasi",
                    NoUrut = 4,
                    NamaAction = "Hapus",
                    IdController = "Eleminasi",
                },

                /* Laporan */
                new ActionViewModel{
                    IdAction = "GetLaporanGabungan",
                    NoUrut = 1,
                    NamaAction = "Lap Gabungan",
                    IdController = "Laporan",
                },
                new ActionViewModel{
                    IdAction = "GetLaporanKonsolidasi",
                    NoUrut = 1,
                    NamaAction = "Lab Konsolidasi",
                    IdController = "Laporan",
                },
                #endregion

            };
        }

        public IEnumerable<MenuViewModel> GetListMenu()
        {
            if (_mvcMenu != null)
                return _mvcMenu;

            _mvcMenu = new List<MenuViewModel>();
            _mvcConntroller = new List<ControllerViewModel>();

            var items = this.MenuApp();
            foreach (var _menu in items)
            {
                var currentModule = new MenuViewModel
                {
                    IdMenu = _menu.IdMenu,
                    NoUrut = _menu.NoUrut,
                    NamaMenu = _menu.NamaMenu,
                };

                var ctr = this.Controller().Where(c => c.IdMenu == _menu.IdMenu).OrderBy(x => x.NoUrut).ToList();
                var controller = new List<ControllerViewModel>();
                foreach (var _ctr in ctr)
                {
                    var act = this.Action().Where(c => c.IdController == _ctr.IdController).OrderBy(x => x.NoUrut).ToList();
                    var action = new List<ActionViewModel>();
                    foreach (var _act in act)
                    {
                        action.Add(new ActionViewModel
                        {
                            IdAction = _act.IdAction,
                            NoUrut = _act.NoUrut,
                            NamaAction = _act.NamaAction,
                            IdController = _act.IdController,
                        });
                    }

                    controller.Add(new ControllerViewModel
                    {
                        IdController = _ctr.IdController,
                        NoUrut = _ctr.NoUrut,
                        Controller = _ctr.Controller,
                        IdMenu = _ctr.IdMenu,
                        ActionViewModel = action,
                    });

                }

                if (controller.Any())
                {
                    currentModule.ControllerViewModel = controller;
                    _mvcMenu.Add(currentModule);
                }
            }
            return _mvcMenu;

        }


    }
}
