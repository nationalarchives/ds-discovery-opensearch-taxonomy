using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace NationalArchives.Taxonomy.Common
{
    public sealed class TestCategoriseSingleRequest
    {
        private string _title;

        //@JsonProperty(required = true)
        private string _description;

        private string _contextDescription;

        private string _docReference;
        private string _catDocRef;
        private string[] _corpBodys;
        private string[] _subjects;
        //@JsonProperty(value = "placeName")
        private string[] _placeNames;
        //@JsonProperty(value = "personFullName")
        private string[] _personFullnames;
        private string _coveringDates;

        public String Title
        {
            get => _title;
            set => _title = value;
        }

        public String Description
        {
            get => _description;
            set => _description = value;
        }

        public String ContextDescription
        {
            get => _contextDescription;
            set => _contextDescription = value;
        }

        public String DocReference
        {
            get => _docReference;
            set => _docReference = value;
        }


        public String CatDocRef
        {
            get => _catDocRef;
            set => _catDocRef = value;
        }

        public IList<string> CorpBodys
        {
            get => new ReadOnlyCollection<string>(_corpBodys ?? new string[]{ });
            set => _corpBodys = value.Cast<string>().ToArray();
        }

        public IList<string> Subjects
        {
            get => new ReadOnlyCollection<string>(_subjects ?? new string[] { });
            set => _subjects = value.Cast<string>().ToArray();
        }

        public IList<string> PlaceNames
        {
            get => new ReadOnlyCollection<string>(_placeNames ?? new string[] { });
            set => _placeNames = value.Cast<string>().ToArray();
        }

        public IList<string> PersonFullnames
        {
            get => new ReadOnlyCollection<string>(_personFullnames ?? new string[] { });
            set => _personFullnames = value.Cast<string>().ToArray();
        }

        public string CoveringDates
        {
            get => _coveringDates;
            set => _coveringDates = value;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("TestCategoriseSingleRequest [title=");
            builder.Append(_title);
            builder.Append(", docReference=");
            builder.Append(_docReference);
            builder.Append(", catDocRef=");
            builder.Append(_catDocRef);
            builder.Append("]");
            return builder.ToString();
        }

        public override int GetHashCode()
        {
            return 17 * this.ToString().GetHashCode();
        }
    }
}
