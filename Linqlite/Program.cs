using System.Linq;
using Hydrator.Linq;
using Hydrator.Models;


var provider = new QueryProvider();  
var photos = new QueryableTable<Photo>(provider);
var gps = new QueryableTable<GpsLocalisation>(provider);
var cam = new QueryableTable<CameraSetting>(provider);


var result = photos.Where(p => p.Filename == "Chat" && (p.Id != 5 || p.Filename.Contains("chien"))).ToList();
var tmp = photos.Join(gps, p => p.Localisation.Latitude, g => g.Latitude, (p, g) => new { p, g }).Where(q => q.p.Id == 12).ToList();
//var test = tmp.FirstOrDefault();
int i = 0;
var res = photos.
    Join(gps, p => p.Localisation.Latitude, g => g.Latitude, (p, g) => new { p, g }).
    Join(cam, q => q.p.Focal, c => c.FocalString, (q, c) => new { q, c }).
    Where(r => r.q.p.Id == 21).ToList();
