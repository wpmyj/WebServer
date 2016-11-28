﻿using System.Collections.Generic;

using DAL.DAO;
using DAL.DAOVO;
using DAL.DAOFactory;
using System.IO;

namespace WebServer.Models.Document
{
    public class DocumentService : Organizor
    {
        public Status addFile(string userName, int agendaID, string fileName, long fileSize, string fileFullPath)
        {
            int filesize = (int)fileSize;//long转为int

            AgendaDAO agendaDao = Factory.getInstance<AgendaDAO>();
            AgendaVO agendaVo = agendaDao.getOne<AgendaVO>(agendaID);
            if (agendaVo == null)
            {
                return Status.FAILURE;
            }
            //初始化会议操作
            meeting_initOperator(agendaVo.meetingID);
            //验证拥有者权限
            if (!meeting_validatePermission(userName))
            {
                return Status.PERMISSION_DENIED;
            }

            //判断会议是否开启，如果开启，更新“会议更新状态”
            if (meeting_isOpening())
            {
                meeting_updateMeetingUpdateStatus();
            }
            else if (meeting_isOpended())//会议已结束,不允许添加
            {
                return Status.FAILURE;
            }

            FileDAO fileDao = Factory.getInstance<FileDAO>();
            Dictionary<string, object> wherelist = new Dictionary<string, object>();
            //获取当前文件的个数
            wherelist.Add("agendaID", agendaID);
            List<FileVO> fileVolist = fileDao.getAll<FileVO>(wherelist);

            //获取新文件的ID
            int fileID = FileDAO.getID();

            //设置新的文件编号
            int fileIndex = fileVolist == null ? 1 : fileVolist.Count + 1;

            //添加文件
            FileVO fileVo = new FileVO
            {
                fileID = fileID,
                fileName = fileName,
                fileIndex = fileIndex,
                fileSize = filesize,
                filePath = fileFullPath,
                agendaID = agendaID
            };

            if (fileDao.insert<FileVO>(fileVo) != 1)
            {
                return Status.FAILURE;
            }
            else
            {
                return Status.SUCCESS;
            }
        }

        //上传文件到服务器端的路径
        public string getFilePath(int agendaID)
        {
            AgendaDAO agendaDao = Factory.getInstance<AgendaDAO>();
            AgendaVO agendaVo = agendaDao.getOne<AgendaVO>(agendaID);
            if (agendaVo == null)//议程不存在，文件路径设置为空
            {
                return null;
            }
            int meetingID = agendaVo.meetingID;
            //上传后文件存储在服务器端的路径
            string filePath = System.Web.HttpContext.Current.Server.MapPath(@"\upfiles\origin\" + meetingID + "\\" + agendaID + "\\");
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            return filePath;
        }

        /// <summary>
        /// 获取指定议程的附件信息
        /// </summary>
        /// <param name="agendaID"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        public Status getAll(int agendaID, out List<Document> documents)
        {
            documents = new List<Document>();

            Dictionary<string, object> wherelist = new Dictionary<string, object>();

            FileDAO fileDao = Factory.getInstance<FileDAO>();
            wherelist.Add("agendaID", agendaID);
            List<FileVO> fileVolist = fileDao.getAll<FileVO>(wherelist);
            if (wherelist == null)
            {
                return Status.NONFOUND;
            }
            foreach(FileVO fileVo in fileVolist){
                documents.Add(
                    new Document
                    {
                        documentID = fileVo.fileID,
                        documentName = fileVo.fileName,
                        documentSize = fileVo.fileSize + "KB",
                        documentPath = fileVo.filePath,
                        agendaID = fileVo.agendaID
                    });
            }

            return Status.SUCCESS;
        }

        public Status deleteMultipe(string userName, List<int> documents)
        {
            if (documents == null || documents.Count == 0)
            {
                return Status.ARGUMENT_ERROR;
            }

            FileDAO fileDao = Factory.getInstance<FileDAO>();
            AgendaDAO agendaDao = Factory.getInstance<AgendaDAO>();
            Dictionary<string, object> wherelist = new Dictionary<string, object>();

            foreach (int documentID in documents)
            {
                //获取附件信息
                FileVO fileVo = fileDao.getOne<FileVO>(documentID);
                if (fileVo == null)
                    continue;

                //获取议程信息
                AgendaVO agendaVo = agendaDao.getOne<AgendaVO>(fileVo.agendaID);
                if (agendaVo == null)
                {
                    continue;
                }
                //初始化会议操作
                meeting_initOperator(agendaVo.meetingID);
                //验证权限
                if (meeting_validatePermission(userName))//有权限就删除
                {
                    //判断会议是否 未开启,如果 不是”未开启“，直接退出
                    if (!meeting_isNotOpen())
                    {
                        return Status.FAILURE;
                    }
                    //更新其他附件的序号信息
                    fileDao.updateIndex(fileVo.agendaID, fileVo.fileIndex);
                    //删除当前附件
                    fileDao.delete(fileVo.fileID);
                }
                else // 无权限，直接退出（已删除的不恢复）
                {
                    return Status.PERMISSION_DENIED;
                }   

            }
            return Status.SUCCESS;
        }

        /// <summary>
        /// 为议程提供服务，删除议程下所有附件
        /// </summary>
        /// <param name="agendaID"></param>
        /// <returns></returns>
        public Status deleteAll(int agendaID)
        {
            Dictionary<string, object> wherelist = new Dictionary<string, object>();
            FileDAO fileDao = Factory.getInstance<FileDAO>();
            wherelist.Add("agendaID", agendaID);
            List<FileVO> fileVolist = fileDao.getAll<FileVO>(wherelist);
            fileDao.delete(wherelist);//删除数据库中附件对应的记录
            if (fileVolist != null)
            {   //删除议程下所有附件
                foreach (FileVO fileVo in fileVolist)
                {
                    deleteFile(fileVo.filePath);
                }
            }
            return Status.SUCCESS;
        }

        /// <summary>
        /// 删除指定路径的文件？？
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static Status deleteFile(string filePath)
        {
            //删除该路径的文件
            FileInfo fi = new FileInfo(filePath);
            fi.Delete();
            return Status.SUCCESS;
        }

        //文件的上传和下载
    }
}