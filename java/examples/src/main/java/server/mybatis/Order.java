package server.mybatis;

public class Order {

    /**
     * Order ID
     */
    private String ID;

    /**
     * Product ID
     */
    private String ProductID;

    /**
     * 产品数量
     */
    private int Amount;

    /**
     * Customer ID
     */
    private String CustomerID;


    public String getID() {
        return ID;
    }

    public void setID(String ID) {
        this.ID = ID;
    }

    public String getProductID() {
        return ProductID;
    }

    public void setProductID(String productID) {
        ProductID = productID;
    }

    public int getAmount() {
        return Amount;
    }

    public void setAmount(int amount) {
        Amount = amount;
    }

    public String getCustomerID() {
        return CustomerID;
    }

    public void setCustomerID(String customerID) {
        CustomerID = customerID;
    }
}
