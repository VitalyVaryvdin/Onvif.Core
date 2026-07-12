using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

using Onvif.Core.Client.Common;
using Onvif.Core.Client.Device;
using Onvif.Core.Client.Imaging;
using Onvif.Core.Client.Media;
using Onvif.Core.Client.Media2;
using Onvif.Core.Client.Ptz;
using Onvif.Core.Client.Security;

namespace Onvif.Core.Client
{
    public static class OnvifClientFactory
    {
        static Binding CreateBinding()
        {
            var binding = new CustomBinding();
            var textBindingElement = new TextMessageEncodingBindingElement
            {
                MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None)
            };
            var httpBindingElement = new HttpTransportBindingElement
            {
                AllowCookies = true,
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue
            };

            binding.Elements.Add(textBindingElement);
            binding.Elements.Add(httpBindingElement);

            return binding;
        }

        public static async Task<DeviceClient> CreateDeviceClientAsync(string host, string username, string password)
        {
            return await CreateDeviceClientAsync(new Uri($"http://{host}/onvif/device_service"), username, password).ConfigureAwait(false);
        }

        public static async Task<DeviceClient> CreateDeviceClientAsync(Uri uri, string username, string password)
        {
            var binding = CreateBinding();
            var endpoint = new EndpointAddress(uri);
            var device = new DeviceClient(binding, endpoint);
            var time_shift = await GetDeviceTimeShift(device).ConfigureAwait(false);

            device = new DeviceClient(binding, endpoint);
            device.ChannelFactory.Endpoint.EndpointBehaviors.Clear();
            device.ChannelFactory.Endpoint.EndpointBehaviors.Add(new SoapSecurityHeaderBehavior(username, password, time_shift));

            // Connectivity Test
            await device.OpenAsync().ConfigureAwait(false);

            return device;
        }

        public static async Task<MediaClient> CreateMediaClientAsync(string host, string username, string password)
        {
            var binding = CreateBinding();
            var device = await CreateDeviceClientAsync(host, username, password).ConfigureAwait(false);
            var caps = await device.GetCapabilitiesAsync([CapabilityCategory.Media]).ConfigureAwait(false);
            var media = new MediaClient(binding, new EndpointAddress(new Uri(caps.Capabilities.Media.XAddr)));

            var time_shift = await GetDeviceTimeShift(device).ConfigureAwait(false);
            media.ChannelFactory.Endpoint.EndpointBehaviors.Clear();
            media.ChannelFactory.Endpoint.EndpointBehaviors.Add(new SoapSecurityHeaderBehavior(username, password, time_shift));

            // Connectivity Test
            await media.OpenAsync().ConfigureAwait(false);

            return media;
        }

        public static async Task<Media2Client> CreateMedia2ClientAsync(string host, string username, string password)
        {
            var binding = CreateBinding();
            var device = await CreateDeviceClientAsync(host, username, password).ConfigureAwait(false);
            var deviceUri = new Uri($"http://{host}");
            var media2Uri = new Uri(deviceUri, "/onvif/Media2");
            var media2 = new Media2Client(binding, new EndpointAddress(media2Uri));

            var time_shift = await GetDeviceTimeShift(device).ConfigureAwait(false);
            media2.ChannelFactory.Endpoint.EndpointBehaviors.Clear();
            media2.ChannelFactory.Endpoint.EndpointBehaviors.Add(new SoapSecurityHeaderBehavior(username, password, time_shift));

            // Connectivity Test
            await media2.OpenAsync().ConfigureAwait(false);

            return media2;
        }

        public static async Task<PTZClient> CreatePTZClientAsync(string host, string username, string password, Action<PTZClient>? configure = null)
        {
            var binding = CreateBinding();
            var device = await CreateDeviceClientAsync(host, username, password).ConfigureAwait(false);
            var caps = await device.GetCapabilitiesAsync([CapabilityCategory.PTZ]).ConfigureAwait(false);
            var ptz = new PTZClient(binding, new EndpointAddress(new Uri(caps.Capabilities.PTZ.XAddr)));

            var time_shift = await GetDeviceTimeShift(device).ConfigureAwait(false);
            ptz.ChannelFactory.Endpoint.EndpointBehaviors.Clear();
            ptz.ChannelFactory.Endpoint.EndpointBehaviors.Add(new SoapSecurityHeaderBehavior(username, password, time_shift));

            configure?.Invoke(ptz);

            // Connectivity Test
            await ptz.OpenAsync().ConfigureAwait(false);

            return ptz;
        }

        public static async Task<ImagingClient> CreateImagingClientAsync(string host, string username, string password)
        {
            var binding = CreateBinding();
            var device = await CreateDeviceClientAsync(host, username, password).ConfigureAwait(false);
            var caps = await device.GetCapabilitiesAsync([CapabilityCategory.Imaging]).ConfigureAwait(false);
            var imaging = new ImagingClient(binding, new EndpointAddress(new Uri(caps.Capabilities.Imaging.XAddr)));

            var time_shift = await GetDeviceTimeShift(device).ConfigureAwait(false);
            imaging.ChannelFactory.Endpoint.EndpointBehaviors.Clear();
            imaging.ChannelFactory.Endpoint.EndpointBehaviors.Add(new SoapSecurityHeaderBehavior(username, password, time_shift));

            // Connectivity Test
            await imaging.OpenAsync().ConfigureAwait(false);

            return imaging;
        }

        public static async Task<TimeSpan> GetDeviceTimeShift(this DeviceClient device)
        {
			try
			{
				var utc = (await device.GetSystemDateAndTimeAsync().ConfigureAwait(false)).UTCDateTime;
				var dt = new System.DateTime(utc.Date.Year, utc.Date.Month, utc.Date.Day, utc.Time.Hour, utc.Time.Minute, utc.Time.Second);
				return dt - System.DateTime.UtcNow;
			}
			catch (Exception e)
			{
				return new TimeSpan(0);
			}
        }
    }
}
