using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;

namespace WpfApplication1
{
    public partial class Lists
    {
        List<Reader> myReaderList = new List<Reader>();
        /// <summary>
        /// TreeView Binding List
        /// </summary>
        public BindingList<Node> listToReturn = new BindingList<Node>();
        /// <summary>
        /// list of all tag router pairs. Need as we can't search BindingList<tag>
        /// </summary>
      //  public List<TagReader> myTagReaderList = new List<TagReader>();  
        /// <summary>
        /// dataGridView Binding List
        /// </summary>
       // public BindingList<TagBind> allTagList = new BindingList<TagBind>();     //changed to binding list for Datagridview binding

        //public CollectionViewSource ViewSource { get; set; } //this is for sorting function

        public MySortableBindingList<TagBind> allTagList { get; private set; }

       // public ICollectionView Customers { get;  set; }
       // public MyCollectionView GroupedCustomers{get;set;}
        public Queue<int> brSequReciveQueue = new Queue<int>();
        public Queue<txMessage> txMessageQueue = new Queue<txMessage>();
        public Queue<Tag> rxMessageQueue = new Queue<Tag>();
        public Queue<Tag> workingTagQueue0 = new Queue<Tag>();
        public Queue<Tag> workingTagQueue1 = new Queue<Tag>();
       

        public Lists()
        {

            allTagList = new MySortableBindingList<TagBind>();
           
            //Tag test = new Tag();
            //test.PktLength = 10;
            //test.ReaderAdd = "testMACread";
            //test.TagAdd = "testMACtag";

            //allTagList.Add(new TagBind(ref test));
            

           // GroupedCustomers = new MyCollectionView(allTagList);
           // GroupedCustomers.GroupDescriptions.Add(new PropertyGroupDescription("zoneID"));

            //added for data sorting
          //  ViewSource = new CollectionViewSource();
           // ViewSource.Source = allTagList;
           
        }

 


        public void upDateMyReaderList(Tag WorkingTag)
        {
            //Node treeItem = null;
            //Node child = null;
            //treeItem = new Reader();

            Reader searchResultR = myReaderList.Find(Rtest => Rtest.ReaderAdd == WorkingTag.ReaderAdd); //ReaderAddTemp);
            Node bindReaderResult = listToReturn.FirstOrDefault(result => result.Name == WorkingTag.ReaderAdd);
            int index = listToReturn.IndexOf(bindReaderResult);
            if (searchResultR == null)       // Reader not in list
            {
                myReaderList.Add(new Reader
                {

                    ReaderAdd = WorkingTag.ReaderAdd,

                    myTagList = new List<Tag>() { new Tag() { TagAdd = WorkingTag.TagAdd, TTL = 10 } },



                });

                //test for binding
                TreeReader treeItem = new TreeReader();

                treeItem.Name = WorkingTag.ReaderAdd;
                treeItem.Children.Add(new TreeTag()
                {
                    Name = WorkingTag.TagAdd,
                    Children = new BindingList<Node>() 
                
                                                       {
                                                       new TreeValue (){TTL = 10, PktLength = WorkingTag.PktLength, PktSequence = WorkingTag.PktSequence, BrSequ = WorkingTag.BrSequ}                                      
                                                       }
                }
                                      );



                System.Windows.Application.Current.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,


                (Action)delegate()
                {
                    listToReturn.Add(treeItem);

                });





            }
            else                // If it is in the list we need to add the Tag to it  if it is not already there. If it is we need to modify it.
            {

                Tag searchResultT = searchResultR.FindTag(WorkingTag.TagAdd);
                Node bindTagResult = listToReturn[index].Children.FirstOrDefault(result2 => result2.Name == WorkingTag.TagAdd);
                int index2 = listToReturn[index].Children.IndexOf(bindTagResult);

                // if (searchResultT == null)          // tag not in list NOTE with this keeps adding tags
                if (bindTagResult == null)
                {//add

                    searchResultR.AddNewTag(ref WorkingTag);

                    System.Windows.Application.Current.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (Action)delegate()
                    {
                        listToReturn[index].Children.Add(new TreeTag()
                        {
                            Name = WorkingTag.TagAdd,
                            endPointType = WorkingTag.endPointType,
                            readerAddress = WorkingTag.ReaderAdd,
                            Children = new BindingList<Node>() 
                                                                {
                                                                  new TreeValue (){TTL = 10, PktLength = WorkingTag.PktLength, PktSequence = WorkingTag.PktSequence, BrSequ = WorkingTag.BrSequ}
                                                                                                                    
                                                                }
                        });



                    });


                }
                else
                {
                    //update TTL of tag

                    //searchResultR.UpdateTag(searchResultT, ref PortArray);
                    if (searchResultT != null)
                    {
                        searchResultT.UpdateTag(ref WorkingTag);
                        searchResultT.TTL = 10;
                    }
                    System.Windows.Application.Current.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (Action)delegate()
                    {
                        listToReturn[index].Children[index2].Children[0].TTL = 10; // .Add(new DBConnect.TagBind(WorkingTag.TagAdd, 4));
                        listToReturn[index].Children[index2].Children[0].PktLength = WorkingTag.PktLength;
                        listToReturn[index].Children[index2].Children[0].PktSequence = WorkingTag.PktSequence;
                        listToReturn[index].Children[index2].Children[0].BrSequ = WorkingTag.BrSequ;

                    });

                    // decAllTtlOflistToReturn();



                }

            }
        }

    }
}
