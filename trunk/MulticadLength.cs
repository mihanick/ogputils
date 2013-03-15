namespace MultiDotNet
{


    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using System.IO;

    //Классы Teigha
    using Teigha.Runtime; // Не работает вместе c Multicad.Runtime
    using HostMgd.EditorInput;
    using Platform = HostMgd;
    using PlatformDb = Teigha;
    using HostMgd.ApplicationServices;
    using Teigha.DatabaseServices;
    using Teigha.Geometry;

    using Multicad;
    using Multicad.DatabaseServices;
    using Multicad.DatabaseServices.StandardObjects;
    using Multicad.Geometry;
    //using Multicad.Runtime;
    using Multicad.AplicationServices;



    namespace Multi
    {
        class MultiMeasurer
        {
            //Общие объекты, с которыми будем работать во всех функциях
            Database acCurDb = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
            Document acCurDoc = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            //mihanick: Наша регистрация почему-то не работает и еще и конфликтует с Teigha.Runtime
            //[CommandMethod("MultiLengthSumma", CommandFlags.NoCheck | CommandFlags.NoPrefix)]

            //Регистрируем команду с помощью платформы
            //[CommandMethod("MultiLengthSumma", CommandFlags.UsePickSet)]
            public void mLineOrPlineLengthSumma()
            {
                // получаем объекты выбором на чертеже
                McObjectId[] idSelecteds = McObjectManager.SelectObjects("Выберите объекты - линия, полилиния");
                if (idSelecteds == null || idSelecteds.Length == 0)
                    return;

                double itogLen = 0; // переменная текущей длины
                foreach (McObjectId currID in idSelecteds)
                {
                    McObject currObj = currID.GetObject(); // получаем объект по его ИД.
                    // далее этот объект необходимо распознать (для этого существует спец. группа классов - нач. на DB)
                    if (currObj is DbLine)
                        itogLen += (currObj as DbLine).Line.Length;
                    else if (currObj is DbPolyline)
                        itogLen += (currObj as DbPolyline).Polyline.Length;
                }


                //Вывести результат в Командную строку
                ed.WriteMessage("Общая длина: " + itogLen.ToString());
            }

        }

    }


}