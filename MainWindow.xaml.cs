using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfGenerateHW.Constructor;

namespace WpfGenerateHW
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        #region field

        private TiaPortal portal = null;
        private Project project = null;
        private Device device = null;
        private DirectoryInfo path = null;
        private IoSystem system = null;
        private IList<Device> subdevice = null;
        private IList<Device> Devices = null;
        private IList<addDevice>  addDev = new List<addDevice>();
        public addDevice add;

        #endregion

        public struct addDevice
        {
            public string name;
            public string author;
            public string ipAddress;
            public string identifier;
            public string comment;
        }


        public MainWindow()
        {
            InitializeComponent();
            ObjectIdentifier.ReadSettings(lbMessage);
            Controller.ReadSettings(lbMessage);

            tbPlcName.Text = "PLC_teste";
            tbPlcAuthor.Text = "teste";
            tbPlcIPAddress.Text = "192.168.10.10";
            tbPlcIdentifier.Text = "PLC_CPU";

            tbIHMName.Text = "IHM_teste";
            tbIHMAuthor.Text = "teste";
            tbIHMIPAddress.Text = "192.168.10.100";
            tbIHMIdentifier.Text = "TP1500_X0";

            tbComment.Text = "Standard Comment";
            tbPath.Text = "E:\\Teste\\";
            cbDelete.IsChecked = true;


        }

        private void GenerateDevice()
        {
            Dispatcher.Invoke(() =>
            {
                if (Directory.Exists(tbPath.Text + tbPlcName.Text))
                {

                    try
                    {
                        if (cbDelete.IsChecked == true)
                        {
                            Directory.Delete(tbPath.Text + tbPlcName.Text, true);
                            lbMessage.Items.Insert(0, DateTime.Now.ToString() + " Project" + tbPlcName.Name + " was deleted");
                        }
                    }
                    catch (Exception ex)
                    {
                        lbMessage.Items.Insert(0, DateTime.Now.ToString() + " " + ex.Message);
                    }
                }
            });

            using (portal = new TiaPortal(TiaPortalMode.WithUserInterface))
            {
                Dispatcher.Invoke(() => 
                {
                    path = new DirectoryInfo(tbPath.Text);
                    project = portal.Projects.Create(path, tbPlcName.Text);
                    lbMessage.Items.Insert(0, DateTime.Now.ToString() + " Project" + project.Name + " was created");
                });

                IoDevice(Controller.Devices);
                createDevices(project);

                foreach(var a in project.UngroupedDevicesGroup.Devices)
                {
                    Dispatcher.Invoke(() =>
                    {
                        FindAttributeSet(a.DeviceItems, "Author", tbPlcAuthor.Text);
                        FindAttributeSet(a.DeviceItems, "Comment", tbComment.Text + " " + a.Name);
                    });
                    
                }

                AddToSubnet(Controller.Devices, system);
                SaveProject();
            }
        }

        public void createDevices(Project project)
        {
            Devices = new List<Device>();
            NetworkInterface network = null;

            Dispatcher.Invoke(() =>
            {
                add = new addDevice
                {
                    name = tbIHMName.Text,
                    author = tbIHMAuthor.Text,
                    ipAddress = tbIHMIPAddress.Text,
                    identifier = tbIHMIdentifier.Text,
                    comment = tbComment.Text,
                };
                addDev.Add(add);

                add = new addDevice
                {
                    name = tbPlcName.Text,
                    author = tbPlcAuthor.Text,
                    ipAddress = tbPlcIPAddress.Text,
                    identifier = tbPlcIdentifier.Text,
                    comment = tbComment.Text,
                };
                addDev.Add(add);
            });

            foreach (var a in addDev)
            {
                var device = project.Devices.CreateWithItem("OrderNumber:" + ObjectIdentifier.Identifier[a.identifier], a.name, a.name);
                Devices.Add(device);

                Dispatcher.Invoke(() =>
                {
                    lbMessage.Items.Insert(0, DateTime.Now.ToString() + " Device " + device.Name + " was created");

                    FindAttributeSet(device.DeviceItems, "Author", tbPlcAuthor.Text);
                    FindAttributeSet(device.DeviceItems, "Comment", tbComment.Text + " " + device.Name);
                });
                

                network = FindNetworkInterface(device.DeviceItems);
                network.Nodes.Last().ConnectToSubnet(project.Subnets.Find("GlobalNetwork"));
                network.Nodes.Last().SetAttribute("Address", a.ipAddress);

            }

            var controller = network.IoControllers.First();
            if (controller == null) return;
            system = controller.CreateIoSystem("GeneralSystem");

        }

        private void SaveProject()
        {
            if(project != null)
            {
                project.Save();
                Dispatcher.Invoke(() => lbMessage.Items.Insert(0, DateTime.Now.ToString() + " Project" + project.Name + " was saved"));
            }
        }

        private void IoDevice(IList<Tuple<Controller._Device, IList<Controller._IoCard>>> devices)
        {
            subdevice = new List<Device>();
            var subnet = project.Subnets.Create("System:Subnet.Ethernet", "GlobalNetwork");

            foreach(var d in devices)
            {
                var device = project.UngroupedDevicesGroup.Devices.CreateWithItem("OrderNumber:" + ObjectIdentifier.Identifier[d.Item1.identifier], d.Item1.name, d.Item1.name);
                subdevice.Add(device);
                Dispatcher.Invoke(() =>
                {
                    FindAttributeSet(device.DeviceItems, "Author", tbPlcAuthor.Text);
                    FindAttributeSet(device.DeviceItems, "Comment", tbComment.Text + " " + device.Name);
                });
                


                fillIOController(device, d.Item2);
                var network = FindNetworkInterface(device.DeviceItems);
                network.Nodes.Last().ConnectToSubnet(subnet);
            }
        }

        private void fillIOController(Device device, IList<Controller._IoCard> controller)
        {
            int index = 1;
            DeviceItem rail = FindRail(device.DeviceItems);
            foreach(var card in controller)
            {
                if (card.name.Equals(string.Empty)) continue;
                for(int i = index; i <= controller.Count; i++)
                {
                    if (!rail.CanPlugNew("OrderNumber:" + ObjectIdentifier.Identifier[card.identifier], card.name + "_" + card.position, i)) continue;
                    index = i;
                    rail.PlugNew("OrderNumber:" + ObjectIdentifier.Identifier[card.identifier], card.name + "_" + card.position, i);
                    break;
                }
            }
        }

        private void AddToSubnet(IList<Tuple<Controller._Device, IList<Controller._IoCard>>> devices, IoSystem system)
        {
            foreach(var d in project.UngroupedDevicesGroup.Devices)
            {
                var plcName = Dispatcher.Invoke(() => tbPlcName.Text);
                if (d.Name.Contains(plcName)) continue;

                var network = FindNetworkInterface(d.DeviceItems);
                network.IoConnectors.Last().ConnectToIoSystem(system);

                foreach(var v in devices)
                {
                    if(d.Name.Equals(v.Item1.name) && v.Item1.IP.Equals(string.Empty))
                    {
                        network.Nodes.First().SetAttribute("Address", v.Item1.IP);
                    }

                }

            }
        }

        #region Helper function

        private DeviceItem FindRail(DeviceItemComposition devices)
        {
            foreach(var item in devices)
            {
                if (item.Name.Contains("Rack")) return item;
                return FindRail(item.DeviceItems);
            }
            return null;
        }

        private NetworkInterface FindNetworkInterface(DeviceItemComposition devices)
        {
            NetworkInterface network = null;
            foreach(var device in devices)
            {
                if (network != null) continue;
                network = device.GetService<NetworkInterface>();
                if(network != null)
                {
                    if (findAttributeGet(device, "InterfaceType") != "Ethernet") network = null;
                    else if(network.Nodes == null) network = null;
                    else if(network.Nodes.Count == 0) network = null;
                }
                if (network == null) network = FindNetworkInterface(device.DeviceItems);

            }
            return network; 
        }

        private string findAttributeGet(DeviceItem device, string attribute)
        {
            var list = device.GetAttributeInfos();
            foreach(var item in list)
            {
                if (item.Name.Equals(attribute))
                {
                    return device.GetAttribute(attribute).ToString();
                }
            }
            return string.Empty;
        }

        private void FindAttributeSet(DeviceItemComposition devices, string attribute, object value)
        {
            foreach (var d in devices)
            {
                var list = d.GetAttributeInfos();
                foreach (var item in list)
                {
                    if (item.Name.Equals(attribute))
                    {
                        d.SetAttribute(attribute, value);
                    }
                }
            }
        } 

        #endregion

        #region Events
        private async void btnGenHw_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(GenerateDevice);
        }

        private void btnReloadCsv_Click(object sender, RoutedEventArgs e)
        {
            ObjectIdentifier.ReadSettings(lbMessage);
            Controller.ReadSettings(lbMessage);
        }
        #endregion
    }
}
