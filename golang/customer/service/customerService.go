package service

import (
	pb "daprdemos/golang/customer/protos/customer_v1"
)

type CustomerService struct {
}

func (s *CustomerService) GetCustomerById(req *pb.IdRequest) pb.Customer {
	return pb.Customer{
		Id:   req.Id,
		Name: "小红",
	}
}
