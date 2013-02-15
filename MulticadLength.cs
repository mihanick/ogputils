using Teigha.Runtime;
using HostMgd.EditorInput;
using Platform = HostMgd;
using PlatformDb = Teigha;
using HostMgd.ApplicationServices;
using Teigha.DatabaseServices;

using NativePlatform = Teigha;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using Multicad;
using Multicad.DatabaseServices;
using Multicad.DatabaseServices.StandardObjects;
using Multicad.Geometry;
using Multicad.AplicationServices;	


namespace DotNetSample
{


    class ForMisha
	{
        Database acCurDb = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
        Document acCurDoc = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

        Editor ed = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;	
        
        // здесь прописываем имя метода, и каким именем его звать из командной строки (здесь они одинаковы)
        //Закомментил чтобы не светилась
		//[CommandMethod("MultiLengthSumma")]
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
			//MessageBox.Show(itogLen.ToString(), "Длина всех линий и полилиний:", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
