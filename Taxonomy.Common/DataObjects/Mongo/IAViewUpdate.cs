using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Mongo
{
    public class IAViewUpdate
    {
        public static readonly String FIELD_ID = "_id";
        public static readonly String FIELD_CREATIONDATE = "creationDate";
        public static readonly String FIELD_DOCREFERENCE = "docReference";

        private ObjectId _id;

        private DateTime _creationDate;

        private string _docReference;

        private List<CategoryLight> _categories;
        private string _catDocRef;

        //TODO: Why are we calling the base, not a subclass?
        public IAViewUpdate() : base()
        {
           
        }

        //BNO: Again why are we calling the base constructor?
        public IAViewUpdate(IAViewUpdate other) : base()
        {
            this._id = other._id;
            this._creationDate = other._creationDate;
            this._docReference = other._docReference;
            this._categories = other._categories;
            this._catDocRef = other._catDocRef;
        }

        //public string DocReference
    }
}
